using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Authorization;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services;

public class WorkspaceAccessService(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository) : IWorkspaceAccessService
{
    public async Task<(Guid UserId, string Email)> GetCurrentUserInfoAsync(CancellationToken ct = default)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new { Id = (Guid?)u.Id, u.Email },
            ct);

        if (userInfo == null || userInfo.Id == null)
        {
            throw new ForbiddenException("User is not authenticated.");
        }

        return (userInfo.Id.Value, userInfo.Email);
    }

    public async Task<(Guid UserId, string Email, WorkspaceRole Role)> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var userInfo = await GetCurrentUserInfoAsync(ct);
        var role = await workspaceRepository.GetUserRoleAsync(workspaceId, userInfo.UserId, ct);

        if (role == null)
        {
            throw new ForbiddenException("Workspace not found or you don't have permission to view it.");
        }

        return new (userInfo.UserId,  userInfo.Email, role.Value);
    }

    public async Task<(Guid UserId, string Email)> EnsureCanManageWorkspaceAsync(Guid workspaceId,
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

    public async Task EnsureCanManageBoardMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanManageBoardRoles,
            "You don't have permission to manage board roles.", ct);
    }

    public async Task EnsureCanManageSubscriptionsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(workspaceId, WorkspaceRolePermissions.CanManageSubscriptions,
            "You don't have permission to manage subscriptions.", ct);
    }

    private async Task<(Guid UserId, string Email)> EnsureAccessAsync(Guid workspaceId,
        Func<WorkspaceRole, bool> permissionCheck, string errorMessage, CancellationToken ct)
    {
        var userInfo = await GetCurrentUserInfoAsync(ct);
        var userRole = await workspaceRepository.GetUserRoleAsync(workspaceId, userInfo.UserId, ct);

        if (userRole == null)
        {
            throw new ForbiddenException("You are not a member of this workspace.");
        }

        if (!permissionCheck(userRole.Value))
        {
            throw new ForbiddenException(errorMessage);
        }

        return userInfo;
    }
}
