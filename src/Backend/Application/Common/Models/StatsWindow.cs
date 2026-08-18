using Application.Interfaces.Services;
using Contracts.Enums;

namespace Application.Common.Models;

// Start and End are UTC instants for querying; LocalStart and LocalEnd carry the configured zone's offset so
// the response can label the range. End is exclusive.
public record StatsWindow(
    StatsPeriodDto Period,
    DateTimeOffset? Start,
    DateTimeOffset End,
    DateTimeOffset? PreviousStart,
    DateTimeOffset? PreviousEnd,
    DateTimeOffset? LocalStart,
    DateTimeOffset LocalEnd,
    DateOnly FirstDay,
    DateOnly LastDay)
{
    public bool IsAllTime => Start is null;

    public bool HasPreviousWindow => PreviousStart is not null;

    // A period is a run of whole days in the configured zone, so every reader gets the same window.
    public static StatsWindow Resolve(StatsPeriodDto period, IBusinessCalendar calendar)
    {
        var today = calendar.Today;

        // Exclusive, so today counts in full however late in the day the request arrives.
        var endUtc = calendar.StartOfDayUtc(today.AddDays(1));
        var endLocal = calendar.StartOfDayLocal(today.AddDays(1));

        if (period == StatsPeriodDto.AllTime)
        {
            return new StatsWindow(period, null, endUtc, null, null, null, endLocal, today, today);
        }

        var days = DayCount(period);
        var firstDay = today.AddDays(1 - days);
        var startUtc = calendar.StartOfDayUtc(firstDay);
        var previousStartUtc = calendar.StartOfDayUtc(today.AddDays(1 - days - days));

        return new StatsWindow(
            period, startUtc, endUtc, previousStartUtc, startUtc,
            calendar.StartOfDayLocal(firstDay), endLocal, firstDay, today);
    }

    public static int DayCount(StatsPeriodDto period) => period switch
    {
        StatsPeriodDto.Last7Days => 7,
        StatsPeriodDto.Last14Days => 14,
        StatsPeriodDto.Last30Days => 30,
        _ => 0
    };
}
