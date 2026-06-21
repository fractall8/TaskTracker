using Contracts.DTOs;
using Contracts.Requests;
using Refit;

namespace Services.Api;

public interface IWorkspaceInvitesApi
{
    [Post("/api/workspaces/{workspaceId}/invites")]
    Task<IApiResponse<InviteResultDto>> InviteUserAsync(Guid workspaceId, [Body] InviteUserRequest request, CancellationToken ct = default);

    [Post("/api/workspaces/invites/accept")]
    Task<IApiResponse> AcceptInviteAsync([Body] AcceptInviteRequest request, CancellationToken ct = default);
}
