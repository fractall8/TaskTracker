using Application.Common.Models;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class StatsRepository(TaskTrackerDbContext dbContext) : IStatsRepository
{
    public async Task<StatsCounts> GetCountsAsync(
        Guid workspaceId,
        StatsWindow window,
        DateTimeOffset asOf,
        CancellationToken ct = default)
    {
        var from = window.Start;
        var to = window.End;
        var previousFrom = window.PreviousStart;
        var previousTo = window.PreviousEnd;

        // Grouped so all six counts come back in one round trip, matching AiDataRepository.
        var counts = await ScopedTasks(workspaceId)
            .GroupBy(_ => 1)
            .Select(group => new StatsCounts(
                group.Count(task => (from == null || task.CreatedAt >= from) && task.CreatedAt < to),
                group.Count(task => (from == null || task.CreatedAt >= from) && task.CreatedAt < to
                                    && task.IsCompleted),
                group.Count(task => previousFrom != null
                                    && task.CreatedAt >= previousFrom && task.CreatedAt < previousTo),
                group.Count(task => previousFrom != null
                                    && task.CreatedAt >= previousFrom && task.CreatedAt < previousTo
                                    && task.IsCompleted),
                group.Count(task => !task.IsCompleted && task.DueDate != null && task.DueDate < asOf),
                group.Count(task => !task.IsCompleted && task.DueDate != null && task.DueDate < asOf
                                    && (from == null || task.DueDate >= from))))
            .FirstOrDefaultAsync(ct);

        return counts ?? new StatsCounts(0, 0, 0, 0, 0, 0);
    }

    public async Task<List<TaskCompletionSample>> GetCompletionSamplesAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => task.IsCompleted
                           && task.CompletedAt != null
                           && (from == null || task.CompletedAt >= from)
                           && task.CompletedAt < to)
            .Select(task => new TaskCompletionSample(
                task.CompletedAt!.Value,
                (task.CompletedAt.Value - task.CreatedAt).TotalDays))
            .ToListAsync(ct);

    // Bare timestamps rather than a grouped count: the caller's calendar buckets cannot be expressed in
    // EF, so grouping happens in memory. Bounded by the window, except for all time on a large workspace.
    public async Task<List<DateTimeOffset>> GetCreationTimestampsAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => (from == null || task.CreatedAt >= from) && task.CreatedAt < to)
            .Select(task => task.CreatedAt)
            .ToListAsync(ct);

    // Stats are Owner-only and therefore scoped by workspace with no board-membership join
    // (EPIC 5 Decision 1). Archived boards are excluded from every figure (Decision 3).
    private IQueryable<Domain.Entities.TaskItem> ScopedTasks(Guid workspaceId) =>
        dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Column!.Board!.WorkspaceId == workspaceId
                           && !task.Column.Board.IsArchived);
}
