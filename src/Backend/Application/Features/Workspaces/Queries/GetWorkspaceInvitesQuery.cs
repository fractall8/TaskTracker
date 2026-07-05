using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetWorkspaceInvitesQuery(Guid WorkspaceId) : IRequest<List<WorkspaceInviteDto>>;

public class GetWorkspaceInvitesQueryHandler(
    IWorkspaceInviteRepository inviteRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetWorkspaceInvitesQuery, List<WorkspaceInviteDto>>
{
    public async Task<List<WorkspaceInviteDto>> Handle(GetWorkspaceInvitesQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanManageInvitesAsync(request.WorkspaceId, ct);

        var invites = await inviteRepository.GetByWorkspaceIdAsync(request.WorkspaceId, ct);

        return invites.Select(i => new WorkspaceInviteDto(
            i.Id,
            i.Token,
            i.ExpiresAt,
            i.CreatedAt)).ToList();
    }
}
