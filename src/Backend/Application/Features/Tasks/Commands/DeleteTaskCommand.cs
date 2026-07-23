using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Tasks.Commands;

public record DeleteTaskCommand(Guid BoardId, Guid TaskId) : IRequest;

public class DeleteTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<DeleteTaskCommandHandler> logger)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithColumnAsync(request.TaskId, ct);

        if (task == null)
        {
            return;
        }

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found on this board.");
        }

        var fileUrlsToDelete = await attachmentRepository.GetUrlsByTaskIdAsync(request.TaskId, ct);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await taskRepository.DecrementPositionsAsync(task.ColumnId, task.Position + 1, token);
            await taskRepository.SoftDeleteCascadeAsync(request.TaskId, token);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        foreach (var fileUrl in fileUrlsToDelete)
        {
            try
            {
                await fileService.DeleteFileAsync(fileUrl, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphaned blob for Task {TaskId}: {FileUrl}", request.TaskId, fileUrl);
            }
        }

        var updatedRemainingTasks = await taskRepository.GetTasksByColumnIdAsync(task.ColumnId, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskDeleted,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TaskDeletedPayload(
                task.ColumnId,
                request.TaskId,
                BoardActionPositionMappings.ToTaskPositions(updatedRemainingTasks))), ct);
    }
}

public class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
