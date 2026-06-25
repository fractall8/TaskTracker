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
    public async Task<(Guid Id, string Email)> GetCurrentUserInfoAsync(CancellationToken ct = default)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new { Id = (Guid?)u.Id, u.Email },
            ct);

        if (userInfo == null || userInfo.Id == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return (userInfo.Id.Value, userInfo.Email);
    }

    public async Task<WorkspaceRole> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var userInfo = await GetCurrentUserInfoAsync(ct);
        var role = await workspaceRepository.GetUserRoleAsync(workspaceId, userInfo.Id, ct);

        if (role == null)
        {
            throw new UnauthorizedAccessException("Workspace not found or you don't have permission to view it.");
        }

        return role.Value;
    }

    public async Task<(Guid Id, string Email)> EnsureCanManageWorkspaceAsync(Guid workspaceId,
        CancellationToken ct = default)
    {
        return await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanEditWorkspace,
            "You don't have permission to edit this workspace.", ct);
    }

    public async Task EnsureCanManageMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanManageMembers,
            "You don't have permission to manage members.", ct);
    }

    public async Task EnsureCanChangeMemberRoleAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanChangeMemberRole,
            "You don't have permission to change member roles.", ct);
    }

    public async Task EnsureCanDeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanDeleteWorkspace,
            "You don't have permission to delete this workspace.", ct);
    }

    public async Task EnsureCanManageInvitesAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanManageInvites,
            "You don't have permission to invite users.", ct);
    }

    public async Task EnsureCanManageBoardRolesAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanManageMembers,
            "You don't have permission to manage board roles.", ct);
    }

    private async Task<(Guid Id, string Email)> EnsureAccessAsync(Guid workspaceId,
        Func<WorkspaceRole, bool> permissionCheck, string errorMessage, CancellationToken ct)
    {
        var userInfo = await GetCurrentUserInfoAsync(ct);
        var userRole = await workspaceRepository.GetUserRoleAsync(workspaceId, userInfo.Id, ct);

        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace.");
        }

        if (!permissionCheck(userRole.Value))
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        return userInfo;
    }
}
