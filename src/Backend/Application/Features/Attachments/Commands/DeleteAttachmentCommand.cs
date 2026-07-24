using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Attachments.Commands;

public record DeleteAttachmentCommand(
    Guid BoardId,
    Guid TaskId,
    Guid AttachmentId) : IRequest;

public class DeleteAttachmentCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    ILogger<DeleteAttachmentCommandHandler> logger) : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userInfo = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, cancellationToken);

        var task = await taskRepository.GetTaskWithColumnAsync(request.TaskId, cancellationToken);
        if (task == null || task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found.");
        }

        var attachment = await attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken);
        if (attachment == null)
        {
            throw new NotFoundException("Attachment not found.");
        }

        if (attachment.CreatedById != userInfo.UserId)
        {
            throw new ForbiddenException("You can only delete your own attachments.");
        }

        attachmentRepository.Delete(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await fileService.DeleteFileAsync(attachment.FileUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete attachment blob for Task {TaskId}: {FileUrl}", request.TaskId, attachment.FileUrl);
        }

        var remainingAttachmentsCount = await attachmentRepository.CountAsync(a => a.TaskId == request.TaskId, cancellationToken);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskAttachmentsCountChanged,
            userInfo.UserId,
            dateTimeProvider.UtcNow,
            new TaskAttachmentsCountChangedPayload(request.TaskId, remainingAttachmentsCount)), cancellationToken);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.AttachmentDeleted,
            userInfo.UserId,
            dateTimeProvider.UtcNow,
            new AttachmentDeletedPayload(request.TaskId, request.AttachmentId)), cancellationToken);
    }
}
