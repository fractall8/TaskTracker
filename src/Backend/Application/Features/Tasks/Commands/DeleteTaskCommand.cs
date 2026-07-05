using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Tasks.Commands;

public record DeleteTaskCommand(Guid BoardId, Guid TaskId) : IRequest;

public class DeleteTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IRepository<Attachment, Guid> attachmentRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
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

        var attachments = await attachmentRepository.FindAsync(a => a.TaskId == request.TaskId, ct);
        var urlsToDelete = attachments.Select(attachment => attachment.FileUrl).ToList();

        await taskRepository.DecrementPositionsAsync(task.ColumnId, task.Position + 1, ct);

        taskRepository.Delete(task);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var fileUrl in urlsToDelete)
        {
            try
            {
                await fileService.DeleteFileAsync(fileUrl, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphaned attachment blob: {FileUrl}", fileUrl);
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
