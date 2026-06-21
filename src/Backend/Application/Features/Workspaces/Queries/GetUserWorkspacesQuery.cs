using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
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
        var workspaces = await workspaceRepository.GetUserWorkspacesAsync(currentUserId, cancellationToken);

        return workspaces.Select(w => new WorkspaceDto(w.Id, w.Name, w.Description)).ToList();
    }
}
