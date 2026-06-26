using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record RemoveWorkspaceMemberCommand(Guid WorkspaceId, Guid UserIdToRemove) : IRequest;

public class RemoveWorkspaceMemberCommandHandler(
    IWorkspaceMemberRepository workspaceMemberRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveWorkspaceMemberCommand>
{
    public async Task Handle(RemoveWorkspaceMemberCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanManageMembersAsync(request.WorkspaceId, cancellationToken);

        var targetMember = await workspaceMemberRepository.GetByWorkspaceAndUserIdAsync(request.WorkspaceId, request.UserIdToRemove, cancellationToken)
                           ?? throw new KeyNotFoundException("User is not a member of this workspace.");

        if (request.UserIdToRemove == targetMember.UserId)
        {
            throw new InvalidOperationException("You cannot remove yourself from the board using this command. Use 'Leave Board' action instead.");
        }

        if (targetMember.Role == WorkspaceRole.Owner)
        {
            throw new InvalidOperationException("The Owner cannot be removed from the workspace.");
        }

        workspaceMemberRepository.Delete(targetMember);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
