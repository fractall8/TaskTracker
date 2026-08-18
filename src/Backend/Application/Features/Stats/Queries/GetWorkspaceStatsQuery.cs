using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Stats.Queries;

public record GetWorkspaceStatsQuery(Guid WorkspaceId, StatsPeriodDto Period, int UtcOffsetMinutes)
    : IRequest<WorkspaceStatsDto>;

public class GetWorkspaceStatsQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    IStatsRepository statsRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetWorkspaceStatsQuery, WorkspaceStatsDto>
{
    public async Task<WorkspaceStatsDto> Handle(GetWorkspaceStatsQuery request, CancellationToken ct)
    {
        // Owner only. Every figure on this page aggregates the whole workspace with no per-board
        // membership check, so the gate is the only thing keeping it from being a cross-board read.
        await workspaceAccessService.EnsureCanViewStatsAsync(request.WorkspaceId, ct);

        var now = dateTimeProvider.UtcNow;
        var window = StatsWindow.Resolve(request.Period, request.UtcOffsetMinutes, now);

        var counts = await statsRepository.GetCountsAsync(
            request.WorkspaceId, window, window.OverdueBefore, ct);

        // One fetch spanning both windows: they are contiguous, so the split happens in memory.
        var samples = await statsRepository.GetCompletionSamplesAsync(
            request.WorkspaceId, window.PreviousStart ?? window.Start, window.End, ct);

        var summary = new StatsSummaryDto(
            counts.CreatedInPeriod,
            window.HasPreviousWindow ? counts.CreatedInPreviousPeriod : null,
            Rate(counts.CompletedOfCreatedInPeriod, counts.CreatedInPeriod),
            window.HasPreviousWindow
                ? Rate(counts.CompletedOfCreatedInPreviousPeriod, counts.CreatedInPreviousPeriod)
                : null,
            counts.OverdueNow,
            counts.NewlyOverdue,
            Median(samples.Where(sample => InWindow(sample, window.Start, window.End))),
            window.HasPreviousWindow
                ? Median(samples.Where(sample => InWindow(sample, window.PreviousStart, window.PreviousEnd)))
                : null);

        var createdTimestamps = await statsRepository.GetCreationTimestampsAsync(
            request.WorkspaceId, window.Start, window.End, ct);

        var trend = StatsTrendFactory.Build(
            window,
            request.UtcOffsetMinutes,
            createdTimestamps,
            [.. samples.Where(sample => InWindow(sample, window.Start, window.End)).Select(s => s.CompletedAt)]);

        var boards = await statsRepository.GetBoardBreakdownAsync(
            request.WorkspaceId, window.Start, window.End, ct);

        var tags = await BuildTagsAsync(request.WorkspaceId, ct);

        var workload = await BuildWorkloadAsync(request.WorkspaceId, window.OverdueBefore, ct);
        var contributors = await BuildContributorsAsync(request.WorkspaceId, window, ct);
        var overdue = await BuildOverdueAsync(request.WorkspaceId, window, request.UtcOffsetMinutes,
            counts.OverdueNow, ct);

        return new WorkspaceStatsDto(
            window.Period, window.LocalStart, window.LocalEnd, summary, trend, boards, tags,
            workload, contributors, overdue);
    }

    // Total comes from the summary count rather than a second query, so a truncated list still reports the
    // real figure and the two can never drift apart.
    private async Task<StatsOverdueDto> BuildOverdueAsync(
        Guid workspaceId,
        StatsWindow window,
        int utcOffsetMinutes,
        int total,
        CancellationToken ct)
    {
        var rows = await statsRepository.GetOverdueTasksAsync(
            workspaceId, window.OverdueBefore, _maxOverdueRows, ct);

        return new StatsOverdueDto(
            total,
            [
                .. rows.Select(row => new StatsOverdueTaskDto(
                    row.TaskId,
                    row.Title,
                    row.BoardId,
                    row.BoardName,
                    row.AssigneeName,
                    row.AssigneeAvatarUrl,
                    row.DueDate,
                    window.DaysOverdue(row.DueDate, utcOffsetMinutes)))
            ]);
    }

    // Unassigned is pinned first, then whoever is most at risk. Overdue dominates the sort because an
    // overdue task needs attention regardless of how much else the person is carrying.
    private async Task<List<StatsWorkloadDto>> BuildWorkloadAsync(
        Guid workspaceId,
        DateTimeOffset overdueBefore,
        CancellationToken ct)
    {
        var rows = await statsRepository.GetWorkloadAsync(workspaceId, overdueBefore, ct);

        return
        [
            .. rows
                .Select(row => row.UserId is null ? row with { Name = _unassignedLabel } : row)
                .OrderByDescending(row => row.UserId is null)
                .ThenByDescending(row => row.Overdue)
                .ThenByDescending(row => row.OnTrack)
                .ThenBy(row => row.Name)
        ];
    }

    // Only people who did something in the window appear: a roster padded with zeroes is not a contributor
    // list. Reported and completed come from different columns, so they are merged here by user.
    private async Task<List<StatsContributorDto>> BuildContributorsAsync(
        Guid workspaceId,
        StatsWindow window,
        CancellationToken ct)
    {
        var reported = await statsRepository.GetReportedCountsAsync(workspaceId, window.Start, window.End, ct);
        var completed = await statsRepository.GetCompletedCountsAsync(workspaceId, window.Start, window.End, ct);

        var byUser = new Dictionary<Guid, StatsContributorDto>();

        foreach (var row in reported)
        {
            byUser[row.UserId] = new StatsContributorDto(row.UserId, row.Name, row.AvatarUrl, row.Count, 0);
        }

        foreach (var row in completed)
        {
            byUser[row.UserId] = byUser.TryGetValue(row.UserId, out var existing)
                ? existing with { Completed = row.Count }
                : new StatsContributorDto(row.UserId, row.Name, row.AvatarUrl, 0, row.Count);
        }

        return
        [
            .. byUser.Values
                .OrderByDescending(contributor => contributor.Completed)
                .ThenByDescending(contributor => contributor.Reported)
                .ThenBy(contributor => contributor.Name)
        ];
    }

    // Untagged is folded in and sorted alongside the real tags, so the largest bucket leads whether or not
    // it happens to be "no tag at all". Zero buckets are dropped: an empty slice says nothing.
    private async Task<List<StatsTagDto>> BuildTagsAsync(Guid workspaceId, CancellationToken ct)
    {
        var tags = await statsRepository.GetTagBreakdownAsync(workspaceId, ct);
        var untagged = await statsRepository.CountUntaggedOpenTasksAsync(workspaceId, ct);

        if (untagged > 0)
        {
            tags.Add(new StatsTagDto(null, "untagged", null, untagged));
        }

        return [.. tags.OrderByDescending(tag => tag.OpenTasks).ThenBy(tag => tag.Name)];
    }

    private const string _unassignedLabel = "Unassigned";

    private const int _maxOverdueRows = 50;

    private static bool InWindow(TaskCompletionSample sample, DateTimeOffset? from, DateTimeOffset? to) =>
        (from is null || sample.CompletedAt >= from) && (to is null || sample.CompletedAt < to);

    // Null rather than zero when there is no denominator: 0% would claim nothing got done, when in fact
    // nothing was created.
    private static double? Rate(int completed, int created) =>
        created == 0 ? null : (double)completed / created;

    private static double? Median(IEnumerable<TaskCompletionSample> samples)
    {
        // Clamped at zero: a completion stamped before creation is clock skew, and a negative median
        // would be nonsense on the card.
        var days = samples.Select(sample => Math.Max(0, sample.DaysToComplete)).OrderBy(value => value).ToList();

        if (days.Count == 0)
        {
            return null;
        }

        var middle = days.Count / 2;

        return days.Count % 2 == 1 ? days[middle] : (days[middle - 1] + days[middle]) / 2;
    }
}

public class GetWorkspaceStatsQueryValidator : AbstractValidator<GetWorkspaceStatsQuery>
{
    public GetWorkspaceStatsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();

        RuleFor(x => x.Period).IsInEnum();

        // The real range of civil offsets. Outside it DateTimeOffset itself would throw.
        RuleFor(x => x.UtcOffsetMinutes)
            .InclusiveBetween(StatsWindow.MinUtcOffsetMinutes, StatsWindow.MaxUtcOffsetMinutes)
            .WithMessage("UTC offset must be between -720 and 840 minutes.");
    }
}
