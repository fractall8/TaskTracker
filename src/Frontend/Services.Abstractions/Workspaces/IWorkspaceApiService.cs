using Contracts.DTOs;
using Contracts.Requests.Workspaces;

namespace Services.Abstractions.Workspaces;

public interface IWorkspaceApiService
{
    Task<List<WorkspaceDto>> GetUserWorkspacesAsync(CancellationToken ct = default);
    Task<WorkspaceDetailsDto> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken ct = default);
    Task UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken ct = default);
    Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    Task<PagedList<BoardPreviewDto>> GetAllWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1,
        int pageSize = 24, string? searchTerm = null, CancellationToken ct = default);

    Task<PagedList<BoardPreviewDto>> GetMyWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1,
        int pageSize = 24, string? searchTerm = null, CancellationToken ct = default);

    Task<PagedList<BoardPreviewDto>> GetMyArchivedWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1,
        int pageSize = 24, string? searchTerm = null, CancellationToken ct = default);

    Task<List<WorkspaceMemberDto>> GetWorkspaceUsersAsync(Guid workspaceId, string? searchTerm = null,
        CancellationToken ct = default);

    Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    Task ChangeMemberRoleAsync(Guid workspaceId, Guid userId, ChangeMemberRoleRequest request,
        CancellationToken ct = default);

    Task<List<WorkspaceInviteDto>> GetWorkspaceInvitesAsync(Guid workspaceId, CancellationToken ct = default);
    Task<InviteResultDto?> InviteUserAsync(Guid workspaceId, InviteUserRequest request, CancellationToken ct = default);

    Task UpdateInviteExpirationAsync(Guid workspaceId, Guid inviteId, UpdateInviteExpirationRequest request,
        CancellationToken ct = default);

    Task RevokeInviteAsync(Guid workspaceId, Guid inviteId, CancellationToken ct = default);
    Task AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default);
}
