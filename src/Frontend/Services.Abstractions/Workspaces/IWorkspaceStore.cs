using Contracts.DTOs;
using Contracts.Enums;
using Contracts.Requests;

namespace Services.Abstractions.Workspaces;

public interface IWorkspaceStore
{
    List<WorkspaceDto> Workspaces { get; }

    WorkspaceDetailsDto? CurrentWorkspace { get; }

    bool IsLoading { get; }

    string? ErrorMessage { get; }

    event Action? StateChanged;

    Task LoadUserWorkspacesAsync(CancellationToken ct = default);

    Task LoadWorkspaceDetailsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<WorkspaceDto?> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken ct = default);

    Task UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken ct = default);

    Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    Task ChangeMemberRoleAsync(Guid workspaceId, Guid userId, WorkspaceRoleDto newRole, CancellationToken ct = default);

    Task<InviteResultDto?> InviteUserAsync(Guid workspaceId, InviteUserRequest request, CancellationToken ct = default);

    Task AcceptInviteAsync(string token, CancellationToken ct = default);

    Task<List<UserDto>> GetWorkspaceUsersAsync(Guid workspaceId, string? searchTerm = null, CancellationToken ct = default);

    Task<List<UserSearchDto>> SearchUsersNotInWorkspaceAsync(Guid workspaceId, string? searchTerm = null, CancellationToken ct = default);

    Task<PagedList<BoardPreviewDto>> GetWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1, int pageSize = 24, string? searchTerm = null, CancellationToken ct = default);
}
