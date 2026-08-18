namespace Application.Options;

public class BusinessCalendarOptions
{
    public const string SectionName = "BusinessCalendar";

    // An IANA id, resolved through TimeZoneInfo. Every overdue and due-soon boundary is a day in this zone,
    // so the whole deployment agrees on when a task is late.
    public string TimeZoneId { get; set; } = "Europe/Kyiv";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TimeZoneId))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(TimeZoneId)} must be set.");
        }

        // Fails the container rather than surfacing months later as a quietly wrong overdue count.
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(TimeZoneId)} '{TimeZoneId}' is not a known time zone.", exception);
        }
    }
}
