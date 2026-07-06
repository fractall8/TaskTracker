using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Workspaces.Commands;

public record DeleteWorkspaceCommand(Guid WorkspaceId) : IRequest;

public class DeleteWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceAccessService workspaceAccessService,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteWorkspaceCommandHandler> logger)
    : IRequestHandler<DeleteWorkspaceCommand>
{
    public async Task Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanDeleteWorkspaceAsync(request.WorkspaceId, cancellationToken);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken)
                        ?? throw new KeyNotFoundException("Workspace not found.");

        var fileUrlsToDelete = await attachmentRepository.GetUrlsByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);

        workspaceRepository.Delete(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var fileUrl in fileUrlsToDelete)
        {
            try
            {
                await fileService.DeleteFileAsync(fileUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphaned blob for Workspace {WorkspaceId}: {FileUrl}", request.WorkspaceId, fileUrl);
            }
        }
    }
}
