using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record ChangeWorkspaceMemberRoleCommand(Guid WorkspaceId, Guid UserIdToChange, WorkspaceRole NewRole) : IRequest;

public class ChangeWorkspaceMemberRoleCommandHandler(
    IWorkspaceMemberRepository workspaceMemberRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeWorkspaceMemberRoleCommand>
{
    public async Task Handle(ChangeWorkspaceMemberRoleCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanChangeMemberRoleAsync(request.WorkspaceId, cancellationToken);

        var currentUserId = await workspaceAccessService.GetCurrentUserIdAsync(cancellationToken);

        if (currentUserId == request.UserIdToChange && request.NewRole != WorkspaceRole.Owner)
        {
            throw new InvalidOperationException("The Owner cannot demote themselves. Transfer ownership first.");
        }

        var targetMember = await workspaceMemberRepository.GetByWorkspaceAndUserIdAsync(request.WorkspaceId, request.UserIdToChange, cancellationToken)
                           ?? throw new KeyNotFoundException("User is not a member of this workspace.");

        targetMember.Role = request.NewRole;

        workspaceMemberRepository.Update(targetMember);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
