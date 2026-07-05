using Contracts.DTOs;
using Contracts.Requests.Workspaces;
using Refit;

namespace Services.Api;

public interface IWorkspaceMembersApi
{
    [Get("/api/workspaces/{workspaceId}/users")]
    Task<IApiResponse<List<WorkspaceMemberDto>>> GetWorkspaceUsersAsync(Guid workspaceId, [Query] string? searchTerm = null, CancellationToken ct = default);

    [Delete("/api/workspaces/{workspaceId}/members/{userId}")]
    Task<IApiResponse> RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    [Put("/api/workspaces/{workspaceId}/members/{userId}/role")]
    Task<IApiResponse> ChangeMemberRoleAsync(Guid workspaceId, Guid userId, [Body] ChangeMemberRoleRequest request, CancellationToken ct = default);
}
