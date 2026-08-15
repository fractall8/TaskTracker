using Domain.Enums;

namespace Domain.Authorization;

public static class BoardRolePermissions
{
    public static bool CanEditBoard(BoardRole role) =>
        role is BoardRole.Admin;

    public static bool CanDeleteBoard(BoardRole role) =>
        role is BoardRole.Admin;

    public static bool CanManageColumns(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster;

    public static bool CanManageTasks(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster;

    public static bool CanManageAttachments(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster or BoardRole.User;

    public static bool CanMoveTasks(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster or BoardRole.User;

    // Matches CanMoveTasks, not CanManageTasks: a User can already drag a task into a "Done" column.
    public static bool CanCompleteTasks(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster or BoardRole.User;

    public static bool CanManageMembers(BoardRole role) =>
        role is BoardRole.Admin;

    public static bool CanExportBoard(BoardRole role) =>
        role is BoardRole.Admin;

    public static bool CanManageComments(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster or BoardRole.User;

    public static bool CanViewBoard(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster or BoardRole.User;

    public static bool CanManageCall(BoardRole role) =>
        role is BoardRole.Admin or BoardRole.ScrumMaster;
}
