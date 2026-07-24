namespace Contracts.DTOs;

public record SubscriptionDto(string PlanId,
    string Status,
    bool CancelAtPeriodEnd,
    string StripeCustomerId,
    DateTimeOffset? CurrentPeriodStartAt,
    DateTimeOffset? CurrentPeriodEndAt);
