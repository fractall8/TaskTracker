using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Authorization;
using Domain.Enums;

namespace Application.Services;

public class WorkspaceAccessService(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository) : IWorkspaceAccessService
{
    public async Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new { Id = (Guid?)u.Id },
            ct);

        if (userInfo?.Id == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return userInfo.Id.Value;
    }

    public async Task<WorkspaceRole> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        var role = await workspaceRepository.GetUserRoleAsync(workspaceId, userId, ct);

        if (role == null)
        {
            throw new UnauthorizedAccessException("Workspace not found or you don't have permission to view it.");
        }

        return role.Value;
    }

    public Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken ct = default) =>
        EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanEditWorkspace, "You must be a Workspace Admin or Owner to edit this workspace.", ct);

    public Task EnsureCanManageMembersAsync(Guid workspaceId, CancellationToken ct = default) =>
        EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanManageMembers, "You must be a Workspace Admin or Owner to manage members.", ct);

    public Task EnsureCanChangeMemberRoleAsync(Guid workspaceId, CancellationToken ct = default) =>
        EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanChangeMemberRole, "Only the Owner can change member roles.", ct);

    public Task EnsureCanDeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default) =>
        EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanDeleteWorkspace, "Only the Owner can delete the workspace.", ct);

    public Task EnsureCanInviteUsersAsync(Guid workspaceId, CancellationToken ct = default) =>
        EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanInviteUsers, "You must be a Workspace Admin or Owner to invite users.", ct);

    private async Task EnsureAccessAsync(Guid workspaceId, Func<WorkspaceRole, bool> permissionCheck, string errorMessage, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        var userRole = await workspaceRepository.GetUserRoleAsync(workspaceId, userId, ct);

        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace.");
        }

        if (!permissionCheck(userRole.Value))
        {
            throw new UnauthorizedAccessException(errorMessage);
        }
    }
}
