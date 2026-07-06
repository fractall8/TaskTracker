using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
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
    ILogger<DeleteTaskCommandHandler> logger)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithColumnAsync(request.TaskId, ct);

        if (task == null)
        {
            return;
        }

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }

        var fileUrlsToDelete = await attachmentRepository.GetUrlsByTaskIdAsync(request.TaskId, ct);

        await taskRepository.DecrementPositionsAsync(task.ColumnId, task.Position + 1, ct);

        await taskRepository.SoftDeleteCascadeAsync(request.TaskId, ct);

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
