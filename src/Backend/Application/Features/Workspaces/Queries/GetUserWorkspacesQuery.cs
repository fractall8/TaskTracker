using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetUserWorkspacesQuery : IRequest<List<WorkspaceDto>>;

public class GetUserWorkspacesQueryHandler(
    IWorkspaceRepository workspaceRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository)
    : IRequestHandler<GetUserWorkspacesQuery, List<WorkspaceDto>>
{
    public async Task<List<WorkspaceDto>> Handle(GetUserWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u.Id, cancellationToken);
        var memberships = await workspaceRepository.GetUserWorkspacesWithRolesAsync(currentUserId, cancellationToken);

        return memberships
            .Select(m => new WorkspaceDto(m.Workspace.Id, m.Workspace.Name, m.Workspace.Description, (WorkspaceRoleDto)m.Role))
            .ToList();
    }
}
