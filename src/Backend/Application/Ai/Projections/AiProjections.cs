using Contracts.Enums;

namespace Application.Ai.Projections;

// Everything the AI assistant may read. Governed by tests/ArchitectureTests/Ai/AiProjectionSurface.approved.txt:
// adding a field here fails the build until that file is updated and reviewed. Never add user identity —
// relationships to the caller are booleans resolved server-side

public sealed record AiWorkspaceSummary(
    Guid Id,
    string Name,
    WorkspaceRoleDto MyWorkspaceRole,
    int MyBoardCount);

public sealed record AiWorkspaceUsage(
    Guid Id,
    string Name,
    WorkspaceRoleDto MyWorkspaceRole,
    int BoardCount,
    int ArchivedBoardCount,
    int MemberCount);

public sealed record AiBoardSummary(
    Guid Id,
    string Name,
    bool IsArchived,
    DateTimeOffset? ArchivedAt,
    BoardRoleDto? MyBoardRole,
    int ColumnCount,
    int TaskCount);

public sealed record AiColumnTaskCount(
    Guid Id,
    string Name,
    int Position,
    int TaskCount);

public sealed record AiBoardDetail(
    Guid Id,
    string Name,
    bool IsArchived,
    BoardRoleDto? MyBoardRole,
    int TaskCount,
    int OverdueTaskCount,
    int UnassignedTaskCount,
    IReadOnlyList<AiColumnTaskCount> Columns);

public sealed record AiTaskSummary(
    Guid Id,
    string Title,
    string ColumnName,
    int Position,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsAssigned,
    bool IsAssignedToMe,
    bool IsReportedByMe,
    int AttachmentCount,
    int CommentCount);

public sealed record AiTaskCounts(
    int Total,
    int Overdue,
    int DueThisWeek,
    int AssignedToMe);

public sealed record AiPlanLimits(
    string PlanId,
    string PlanDisplayName,
    int? MaxMembersPerWorkspace,
    int? MaxBoardsPerWorkspace,
    int? MaxColumnsPerBoard,
    int? MaxTasksPerBoard,
    int MaxAttachmentSizeMb,
    bool CanExportBoard,
    int BoardsUsed,
    int MembersUsed);
