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

    public static bool CanManageSubscriptions(WorkspaceRole role) =>
        role is WorkspaceRole.Owner;

    // Renaming, recolouring and deleting change or remove a tag on tasks across every board, so they are
    // curator actions. Creating is not: any member may add to the vocabulary.
    public static bool CanCurateTags(WorkspaceRole role) =>
        role is WorkspaceRole.Admin or WorkspaceRole.Owner;

    // Stats aggregate every board in the workspace without a per-board membership check, which is only
    // safe for a role enrolled on every board by construction. Widening this requires switching the stats
    // queries to a membership filter first (EPIC 5 Decision 1).
    public static bool CanViewStats(WorkspaceRole role) =>
        role is WorkspaceRole.Owner;
}
