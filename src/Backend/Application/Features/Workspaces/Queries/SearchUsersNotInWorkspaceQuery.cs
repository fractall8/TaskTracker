using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record SearchUsersNotInWorkspaceQuery(Guid WorkspaceId, string? SearchTerm) : IRequest<List<UserSearchDto>>;

public class SearchUsersNotInWorkspaceQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    IUserRepository userRepository)
    : IRequestHandler<SearchUsersNotInWorkspaceQuery, List<UserSearchDto>>
{
    public async Task<List<UserSearchDto>> Handle(SearchUsersNotInWorkspaceQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanInviteUsersAsync(request.WorkspaceId, ct);
        return await userRepository.SearchUsersNotInWorkspaceAsync(request.WorkspaceId, request.SearchTerm, ct);
    }
}
