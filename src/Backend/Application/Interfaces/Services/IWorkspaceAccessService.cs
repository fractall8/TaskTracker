using Domain.Enums;

namespace Application.Interfaces.Services;

public interface IWorkspaceAccessService
{
    Task<(Guid UserId, string Email)> GetCurrentUserInfoAsync(CancellationToken ct = default);
    Task<(Guid UserId, string Email, WorkspaceRole Role)> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default);
    Task<(Guid UserId, string Email )> EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanChangeMemberRoleAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanDeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageInvitesAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageBoardMembersAsync(Guid workspaceId, CancellationToken ct = default);
}
