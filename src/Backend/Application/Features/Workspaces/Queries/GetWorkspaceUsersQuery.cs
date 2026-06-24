using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetWorkspaceUsersQuery(Guid WorkspaceId, string? SearchTerm) : IRequest<List<UserDto>>;

public class GetWorkspaceUsersQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    IUserRepository userRepository)
    : IRequestHandler<GetWorkspaceUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetWorkspaceUsersQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        return await userRepository.SearchWorkspaceUsersAsync(request.WorkspaceId, request.SearchTerm, ct);
    }
}
