namespace Domain.Entities;

public class StripeWebhookEvent
{
    public required string EventId { get; init; }

    public required string EventType { get; set; }

    public DateTimeOffset ReceivedAt { get; set; } = DateTime.UtcNow;

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? LastError { get; set; }
}
