using Application.Common.Models;
using Application.Interfaces.Repositories;
using Contracts.DTOs;
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

    // Every active board is returned, including empty ones: an owner who knows a board exists should not
    // have to wonder why it is missing. Busiest first, so the collapsed view shows the boards that matter.
    public async Task<List<StatsBoardDto>> GetBoardBreakdownAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var boards = await dbContext.Boards
            .AsNoTracking()
            .Where(board => board.WorkspaceId == workspaceId && !board.IsArchived)
            .Select(board => new StatsBoardDto(
                board.Id,
                board.Name,
                board.Columns.SelectMany(column => column.Tasks)
                    .Count(task => task.IsCompleted
                                   && task.CompletedAt != null
                                   && (from == null || task.CompletedAt >= from)
                                   && task.CompletedAt < to),
                board.Columns.SelectMany(column => column.Tasks).Count(task => !task.IsCompleted)))
            .ToListAsync(ct);

        // Sorted here rather than in SQL: EF cannot order by a property of a constructed projection, and
        // repeating the count subqueries inside OrderBy would double the query. Bounded by the plan's
        // board limit, so the list is small.
        return [.. boards.OrderByDescending(board => board.CompletedInPeriod + board.OpenNow)
            .ThenBy(board => board.BoardName)];
    }

    // Driven from the task side, so a tag carrying no open work simply does not appear — the panel answers
    // "where is open work concentrated", not "what tags exist".
    public async Task<List<StatsTagDto>> GetTagBreakdownAsync(
        Guid workspaceId,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => !task.IsCompleted)
            .SelectMany(task => task.TaskTags)
            .GroupBy(link => new { link.TagId, link.Tag!.Name, link.Tag.Color })
            .Select(group => new StatsTagDto(
                group.Key.TagId,
                group.Key.Name,
                group.Key.Color,
                group.Count()))
            .ToListAsync(ct);

    public async Task<int> CountUntaggedOpenTasksAsync(Guid workspaceId, CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .CountAsync(task => !task.IsCompleted && !task.TaskTags.Any(), ct);

    // Unassigned tasks group under a null AssigneeId, which is why the bucket falls out of the same query
    // rather than needing a second one. The handler names it.
    public async Task<List<StatsWorkloadDto>> GetWorkloadAsync(
        Guid workspaceId,
        DateTimeOffset asOf,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => !task.IsCompleted)
            .GroupBy(task => new
            {
                task.AssigneeId,
                task.Assignee!.DisplayName,
                task.Assignee.AvatarUrl
            })
            .Select(group => new StatsWorkloadDto(
                group.Key.AssigneeId,
                group.Key.DisplayName,
                group.Key.AvatarUrl,
                group.Count(task => task.DueDate == null || task.DueDate >= asOf),
                group.Count(task => task.DueDate != null && task.DueDate < asOf)))
            .ToListAsync(ct);

    public async Task<List<StatsUserCount>> GetReportedCountsAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => (from == null || task.CreatedAt >= from) && task.CreatedAt < to)
            .GroupBy(task => new { task.ReporterId, task.Reporter!.DisplayName, task.Reporter.AvatarUrl })
            .Select(group => new StatsUserCount(
                group.Key.ReporterId,
                group.Key.DisplayName,
                group.Key.AvatarUrl,
                group.Count()))
            .ToListAsync(ct);

    // CompletedById is nullable: the FK clears on user deletion, so those completions have no contributor.
    public async Task<List<StatsUserCount>> GetCompletedCountsAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => task.IsCompleted
                           && task.CompletedById != null
                           && task.CompletedAt != null
                           && (from == null || task.CompletedAt >= from)
                           && task.CompletedAt < to)
            .GroupBy(task => new { task.CompletedById, task.CompletedBy!.DisplayName, task.CompletedBy.AvatarUrl })
            .Select(group => new StatsUserCount(
                group.Key.CompletedById!.Value,
                group.Key.DisplayName,
                group.Key.AvatarUrl,
                group.Count()))
            .ToListAsync(ct);

    // Oldest due date first, so the worst offenders survive the cap.
    public async Task<List<OverdueTaskRow>> GetOverdueTasksAsync(
        Guid workspaceId,
        DateTimeOffset overdueBefore,
        int take,
        CancellationToken ct = default) =>
        await ScopedTasks(workspaceId)
            .Where(task => !task.IsCompleted && task.DueDate != null && task.DueDate < overdueBefore)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Title)
            .Take(take)
            .Select(task => new OverdueTaskRow(
                task.Id,
                task.Title,
                task.Column!.BoardId,
                task.Column.Board!.Name,
                task.Assignee!.DisplayName,
                task.Assignee.AvatarUrl,
                task.DueDate!.Value))
            .ToListAsync(ct);

    // Stats are Owner-only and therefore scoped by workspace with no board-membership join
    // (EPIC 5 Decision 1). Archived boards are excluded from every figure (Decision 3).
    private IQueryable<Domain.Entities.TaskItem> ScopedTasks(Guid workspaceId) =>
        dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Column!.Board!.WorkspaceId == workspaceId
                           && !task.Column.Board.IsArchived);
}
