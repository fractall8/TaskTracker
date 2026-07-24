namespace Contracts.DTOs;

public record SubscriptionWebhookEventDto(
    string EventId,
    string EventType,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    string? StripePriceId,
    Guid? WorkspaceId,
    string? PlanId,
    string? Status,
    DateTimeOffset? CurrentPeriodStartAt,
    DateTimeOffset? CurrentPeriodEndAt,
    bool CancelAtPeriodEnd,
    Guid? UserId);
