using Application.Ai.Projections;

namespace Application.Interfaces.Repositories;

// Read-only reads for the AI assistant. currentUserId always comes from the authenticated principal via
// the handler, never from the model. No method reads the clock — callers pass asOf, so results are
// deterministic under test.
public interface IAiDataRepository
{
    Task<IReadOnlyList<AiWorkspaceSummary>> GetMyWorkspacesAsync(
        Guid currentUserId,
        CancellationToken ct = default);

    Task<AiWorkspaceUsage?> GetWorkspaceUsageAsync(
        Guid workspaceId,
        Guid currentUserId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AiBoardSummary>> GetBoardsAsync(
        Guid workspaceId,
        Guid currentUserId,
        bool includeArchived,
        CancellationToken ct = default);

    Task<AiBoardDetail?> GetBoardDetailAsync(
        Guid boardId,
        Guid currentUserId,
        DateTimeOffset asOf,
        CancellationToken ct = default);

    Task<IReadOnlyList<AiTaskSummary>> GetBoardTasksAsync(
        Guid boardId,
        Guid currentUserId,
        AiTaskFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyList<AiTaskSummary>> GetWorkspaceOverdueTasksAsync(
        Guid workspaceId,
        Guid currentUserId,
        DateTimeOffset asOf,
        int take,
        CancellationToken ct = default);

    // Not "open" tasks: TaskItem has no completion state, so there is nothing to filter closed ones by.
    Task<AiTaskCounts> CountWorkspaceTasksAsync(
        Guid workspaceId,
        Guid currentUserId,
        Guid? boardId,
        DateTimeOffset asOf,
        CancellationToken ct = default);
}

// Enumerated filters only — never an open expression the model could compose. "Overdue" is expressed as
// DueBefore=now by the caller, which is what keeps the repository clock-free.
public sealed record AiTaskFilter(
    Guid? ColumnId = null,
    bool OnlyAssignedToMe = false,
    DateTimeOffset? DueBefore = null,
    int Take = 25);
