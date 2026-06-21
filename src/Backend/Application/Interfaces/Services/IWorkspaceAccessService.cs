using Domain.Enums;

namespace Application.Interfaces.Services;

public interface IWorkspaceAccessService
{
    Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default);
    Task<WorkspaceRole> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanManageMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanChangeMemberRoleAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanDeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task EnsureCanInviteUsersAsync(Guid workspaceId, CancellationToken ct = default);
}
