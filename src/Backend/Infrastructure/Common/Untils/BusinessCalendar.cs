using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Application.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Common.Untils;

public class BusinessCalendar : IBusinessCalendar
{
    private readonly IDateTimeProvider _clock;
    private readonly TimeZoneInfo _zone;

    public BusinessCalendar(IDateTimeProvider clock, IOptions<BusinessCalendarOptions> options)
    {
        _clock = clock;
        _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    }

    public string TimeZoneId => _zone.Id;

    public DateOnly Today => ToLocalDate(_clock.UtcNow);

    public DateTimeOffset StartOfDayUtc(DateOnly date) => StartOfDayLocal(date).ToUniversalTime();

    public DateTimeOffset StartOfTodayUtc() => StartOfDayUtc(Today);

    // Converted through TimeZoneInfo rather than by adding a fixed offset, so the boundary does not drift by
    // an hour across a daylight-saving transition.
    public DateTimeOffset StartOfDayLocal(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);

        return new DateTimeOffset(midnight, _zone.GetUtcOffset(midnight));
    }

    public DateOnly ToLocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _zone).DateTime);

    public int DaysOverdue(DateTimeOffset dueDate) => Today.DayNumber - ToLocalDate(dueDate).DayNumber;
}
