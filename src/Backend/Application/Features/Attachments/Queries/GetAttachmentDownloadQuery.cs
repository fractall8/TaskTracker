using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Attachments.Queries;

public record GetAttachmentDownloadQuery(Guid BoardId, Guid TaskId, Guid AttachmentId) : IRequest<AttachmentDownloadDto>;

public class GetAttachmentDownloadQueryHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IAttachmentRepository attachmentRepository,
    IFileService fileService
)
    : IRequestHandler<GetAttachmentDownloadQuery, AttachmentDownloadDto>
{
    public async Task<AttachmentDownloadDto> Handle(GetAttachmentDownloadQuery request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, cancellationToken);

        var task = await taskRepository.GetTaskWithColumnAsync(request.TaskId, cancellationToken);
        if (task == null || task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        var attachment = await attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken);
        if (attachment == null)
        {
            throw new KeyNotFoundException("Attachment not found.");
        }

        var downloadUrl = await fileService.GetDownloadUrlAsync(
            attachment.FileUrl,
            attachment.FileName,
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return new AttachmentDownloadDto(downloadUrl, attachment.FileName);
    }
}

