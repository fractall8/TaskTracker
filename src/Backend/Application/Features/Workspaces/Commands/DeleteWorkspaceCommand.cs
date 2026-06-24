using Application.Interfaces;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record DeleteWorkspaceCommand(Guid WorkspaceId) : IRequest;

public class DeleteWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteWorkspaceCommand>
{
    public async Task Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanDeleteWorkspaceAsync(request.WorkspaceId, cancellationToken);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken)
                        ?? throw new KeyNotFoundException("Workspace not found.");

        workspaceRepository.Delete(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
