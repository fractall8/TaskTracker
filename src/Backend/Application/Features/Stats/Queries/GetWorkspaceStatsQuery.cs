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

        var counts = await statsRepository.GetCountsAsync(request.WorkspaceId, window, now, ct);

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

        return new WorkspaceStatsDto(window.Period, window.LocalStart, window.LocalEnd, summary, trend);
    }

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
