using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record RevokeInviteCommand(Guid WorkspaceId, Guid InviteId) : IRequest<Unit>;

public class RevokeInviteCommandHandler(
    IWorkspaceInviteRepository inviteRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeInviteCommand, Unit>
{
    public async Task<Unit> Handle(RevokeInviteCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanManageInvitesAsync(request.WorkspaceId, ct);

        var invite = await inviteRepository.GetByIdAsync(request.InviteId, ct)
                     ?? throw new KeyNotFoundException("Invite not found.");

        if (invite.WorkspaceId != request.WorkspaceId)
        {
            throw new UnauthorizedAccessException("Invite does not belong to this workspace.");
        }

        inviteRepository.Delete(invite);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
