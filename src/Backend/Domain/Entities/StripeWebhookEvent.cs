namespace Domain.Entities;

public class StripeWebhookEvent : BaseEntity<Guid>
{
    public required string EventId { get; init; }

    public required string EventType { get; set; }

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? LastError { get; set; }
}
