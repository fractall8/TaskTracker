using Contracts.Enums;

namespace Application.Common.Models;

// Both bounds are UTC instants for querying; LocalStart and LocalEnd carry the caller's offset so the
// response can label the range the way the caller sees it. End is exclusive.
public record StatsWindow(
    StatsPeriodDto Period,
    DateTimeOffset? Start,
    DateTimeOffset End,
    DateTimeOffset? PreviousStart,
    DateTimeOffset? PreviousEnd,
    DateTimeOffset? LocalStart,
    DateTimeOffset LocalEnd)
{
    public const int MinUtcOffsetMinutes = -720;

    public const int MaxUtcOffsetMinutes = 840;

    public bool IsAllTime => Start is null;

    public bool HasPreviousWindow => PreviousStart is not null;

    // A period is a run of whole calendar days in the caller's timezone, not a rolling block of hours:
    // a bucket labelled "Aug 15" has to mean the caller's Aug 15 (EPIC 5 Decision 4a).
    public static StatsWindow Resolve(StatsPeriodDto period, int utcOffsetMinutes, DateTimeOffset nowUtc)
    {
        var offset = TimeSpan.FromMinutes(utcOffsetMinutes);
        var today = nowUtc.ToOffset(offset).Date;

        // Exclusive, so today counts in full however late in the day the request arrives.
        var localEnd = new DateTimeOffset(today.AddDays(1), offset);

        if (period == StatsPeriodDto.AllTime)
        {
            return new StatsWindow(period, null, localEnd.ToUniversalTime(), null, null, null, localEnd);
        }

        var days = DayCount(period);
        var localStart = new DateTimeOffset(today.AddDays(1 - days), offset);
        var localPreviousStart = new DateTimeOffset(today.AddDays(1 - days - days), offset);

        // Query bounds are normalised to UTC: Npgsql refuses a DateTimeOffset with a non-zero offset when
        // writing to timestamptz, so only the Local* pair keeps the caller's offset for labelling.
        return new StatsWindow(
            period,
            localStart.ToUniversalTime(),
            localEnd.ToUniversalTime(),
            localPreviousStart.ToUniversalTime(),
            localStart.ToUniversalTime(),
            localStart,
            localEnd);
    }

    public static int DayCount(StatsPeriodDto period) => period switch
    {
        StatsPeriodDto.Last7Days => 7,
        StatsPeriodDto.Last14Days => 14,
        StatsPeriodDto.Last30Days => 30,
        _ => 0
    };
}
