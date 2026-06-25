using Domain.Enums;

namespace Application.Interfaces.Services;

public interface IWorkspaceAccessService
{
    Task<(Guid Id, string Email)> GetCurrentUserInfoAsync(CancellationToken ct = default);
    Task<WorkspaceRole> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default);
    Task<(Guid Id, string Email )> EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanChangeMemberRoleAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanDeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageInvitesAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageBoardRolesAsync(Guid workspaceId, CancellationToken ct = default);
}
