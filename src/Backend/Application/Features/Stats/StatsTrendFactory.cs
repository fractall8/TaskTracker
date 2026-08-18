using Application.Common.Models;
using Contracts.DTOs;
using Contracts.Enums;

namespace Application.Features.Stats;

// Bucketing happens here rather than in SQL: grouping by calendar day in an arbitrary caller offset needs
// date_trunc(... AT TIME ZONE ...), which EF Core cannot express. The row count is bounded by the window,
// so the timestamps are cheap to fetch and group in memory.
internal static class StatsTrendFactory
{
    private const int MaxDaysForDailyBuckets = 31;

    private const int MaxDaysForWeeklyBuckets = 182;

    public static StatsTrendDto Build(
        StatsWindow window,
        int utcOffsetMinutes,
        IReadOnlyCollection<DateTimeOffset> created,
        IReadOnlyCollection<DateTimeOffset> completed)
    {
        var offset = TimeSpan.FromMinutes(utcOffsetMinutes);

        // End is exclusive, so the last day inside the window is one tick earlier.
        var lastDay = window.LocalEnd.AddTicks(-1).ToOffset(offset).Date;

        if (!TryGetFirstDay(window, offset, created, completed, out var firstDay))
        {
            return new StatsTrendDto(StatsTrendBucketDto.Day, []);
        }

        var bucket = ChooseBucket(firstDay, lastDay);

        var createdPerBucket = CountPerBucket(created, offset, bucket);
        var completedPerBucket = CountPerBucket(completed, offset, bucket);

        var points = new List<StatsTrendPointDto>();

        for (var start = StartOfBucket(firstDay, bucket); start <= lastDay; start = NextBucket(start, bucket))
        {
            createdPerBucket.TryGetValue(start, out var createdCount);
            completedPerBucket.TryGetValue(start, out var completedCount);

            points.Add(new StatsTrendPointDto(new DateTimeOffset(start, offset), createdCount, completedCount));
        }

        return new StatsTrendDto(bucket, points);
    }

    // A fixed period starts where the window starts. All time starts at the first sign of activity, so an
    // empty workspace yields no points and a young one is not padded with months of zeroes.
    private static bool TryGetFirstDay(
        StatsWindow window,
        TimeSpan offset,
        IReadOnlyCollection<DateTimeOffset> created,
        IReadOnlyCollection<DateTimeOffset> completed,
        out DateTime firstDay)
    {
        if (window.LocalStart is { } start)
        {
            firstDay = start.ToOffset(offset).Date;
            return true;
        }

        if (created.Count == 0 && completed.Count == 0)
        {
            firstDay = default;
            return false;
        }

        var earliest = created.Concat(completed).Min();
        firstDay = earliest.ToOffset(offset).Date;

        return true;
    }

    // Keeps the axis readable: a year of daily points is unreadable, a week of monthly points is useless.
    private static StatsTrendBucketDto ChooseBucket(DateTime firstDay, DateTime lastDay)
    {
        var days = (lastDay - firstDay).TotalDays + 1;

        return days switch
        {
            <= MaxDaysForDailyBuckets => StatsTrendBucketDto.Day,
            <= MaxDaysForWeeklyBuckets => StatsTrendBucketDto.Week,
            _ => StatsTrendBucketDto.Month
        };
    }

    private static Dictionary<DateTime, int> CountPerBucket(
        IReadOnlyCollection<DateTimeOffset> timestamps,
        TimeSpan offset,
        StatsTrendBucketDto bucket) =>
        timestamps
            .GroupBy(timestamp => StartOfBucket(timestamp.ToOffset(offset).Date, bucket))
            .ToDictionary(group => group.Key, group => group.Count());

    private static DateTime StartOfBucket(DateTime day, StatsTrendBucketDto bucket) => bucket switch
    {
        // Monday, so a week bucket reads as a working week.
        StatsTrendBucketDto.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        StatsTrendBucketDto.Month => new DateTime(day.Year, day.Month, 1),
        _ => day
    };

    private static DateTime NextBucket(DateTime start, StatsTrendBucketDto bucket) => bucket switch
    {
        StatsTrendBucketDto.Week => start.AddDays(7),
        StatsTrendBucketDto.Month => start.AddMonths(1),
        _ => start.AddDays(1)
    };
}
