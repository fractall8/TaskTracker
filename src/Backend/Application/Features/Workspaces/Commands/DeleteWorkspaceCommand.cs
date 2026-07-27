using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Workspaces.Commands;

public record DeleteWorkspaceCommand(Guid WorkspaceId) : IRequest;

public class DeleteWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceAccessService workspaceAccessService,
    IBoardCallRepository boardCallRepository,
    IBoardCallLifecycleService boardCallLifecycleService,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    ILogger<DeleteWorkspaceCommandHandler> logger)
    : IRequestHandler<DeleteWorkspaceCommand>
{
    public async Task Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanDeleteWorkspaceAsync(request.WorkspaceId, cancellationToken);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace not found.");

        // Every board's ACS room (and any still-connected participants) must be released before the
        // workspace disappears from under them — otherwise they're never cleaned up, since nothing else
        // would ever call EndCallAsync for a call whose board/workspace no longer exists.
        var activeCalls = await boardCallRepository.GetActiveCallsForWorkspaceAsync(request.WorkspaceId, cancellationToken);

        foreach (var activeCall in activeCalls)
        {
            await boardCallLifecycleService.EndCallAsync(activeCall.Id, ct: cancellationToken);
        }

        var fileUrlsToDelete = await attachmentRepository.GetUrlsByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);

        await workspaceRepository.SoftDeleteCascadeAsync(request.WorkspaceId, cancellationToken);

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
