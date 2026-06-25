using Contracts.DTOs;
using Contracts.Requests.Workspaces;
using Refit;

namespace Services.Api;

public interface IWorkspaceInvitesApi
{
    [Get("/api/workspaces/{workspaceId}/invites")]
    Task<IApiResponse<List<WorkspaceInviteDto>>> GetWorkspaceInvitesAsync(Guid workspaceId, CancellationToken ct = default);

    [Post("/api/workspaces/{workspaceId}/invites")]
    Task<IApiResponse<InviteResultDto>> InviteUserAsync(Guid workspaceId, [Body] InviteUserRequest request, CancellationToken ct = default);

    [Put("/api/workspaces/{workspaceId}/invites/{inviteId}/expiration")]
    Task<IApiResponse> UpdateInviteExpirationAsync(Guid workspaceId, Guid inviteId, [Body] UpdateInviteExpirationRequest request, CancellationToken ct = default);

    [Delete("/api/workspaces/{workspaceId}/invites/{inviteId}")]
    Task<IApiResponse> RevokeInviteAsync(Guid workspaceId, Guid inviteId, CancellationToken ct = default);

    [Post("/api/workspaces/invites/accept")]
    Task<IApiResponse> AcceptInviteAsync([Body] AcceptInviteRequest request, CancellationToken ct = default);
}
