namespace Application.Interfaces.Services;

// Due dates are stored as an instant but mean a day, so every consumer has to agree on which day an instant
// falls in. One configured zone decides, rather than each caller guessing.
//
// The Utc and Local variants are named apart on purpose: Npgsql refuses a DateTimeOffset with a non-zero
// offset when writing to timestamptz, so anything reaching a query must be the Utc one.
public interface IBusinessCalendar
{
    string TimeZoneId { get; }

    DateOnly Today { get; }

    // Local midnight of the given day as a UTC instant. Safe to pass to a query.
    DateTimeOffset StartOfDayUtc(DateOnly date);

    DateTimeOffset StartOfTodayUtc();

    // The same moment carrying the zone's offset. For labelling a response, never for querying.
    DateTimeOffset StartOfDayLocal(DateOnly date);

    DateOnly ToLocalDate(DateTimeOffset instant);

    // Whole days between a due date and today. At least 1 for anything that counts as overdue.
    int DaysOverdue(DateTimeOffset dueDate);
}
