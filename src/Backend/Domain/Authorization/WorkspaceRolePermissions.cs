using Domain.Enums;

namespace Domain.Authorization;

public static class WorkspaceRolePermissions
{
    public static bool CanEditWorkspace(WorkspaceRole role) =>
        role is WorkspaceRole.Admin or WorkspaceRole.Owner;

    public static bool CanManageMembers(WorkspaceRole role) =>
        role is WorkspaceRole.Admin or WorkspaceRole.Owner;

    public static bool CanChangeMemberRole(WorkspaceRole role) =>
        role is WorkspaceRole.Owner;

    public static bool CanDeleteWorkspace(WorkspaceRole role) =>
        role is WorkspaceRole.Owner;

    public static bool CanManageInvites(WorkspaceRole role) =>
        role is WorkspaceRole.Owner;

    public static bool CanManageBoardRoles(WorkspaceRole role) =>
        role is WorkspaceRole.Owner or WorkspaceRole.Admin;
}
