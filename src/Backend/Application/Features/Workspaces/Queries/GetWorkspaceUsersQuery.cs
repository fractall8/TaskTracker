using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetWorkspaceUsersQuery(Guid WorkspaceId, string? SearchTerm) : IRequest<List<WorkspaceMemberDto>>;

public class GetWorkspaceUsersQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceMemberRepository workspaceMemberRepository)
    : IRequestHandler<GetWorkspaceUsersQuery, List<WorkspaceMemberDto>>
{
    public async Task<List<WorkspaceMemberDto>> Handle(GetWorkspaceUsersQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        return await workspaceMemberRepository.SearchWorkspaceUsersAsync(request.WorkspaceId, request.SearchTerm, ct);
    }
}
