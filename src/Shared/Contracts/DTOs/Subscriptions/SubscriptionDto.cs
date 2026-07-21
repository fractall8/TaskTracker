namespace Contracts.DTOs;

public record SubscriptionDto(string PlanId,
    string Status,
    bool CancelAtPeriodEnd,
    DateTimeOffset? CurrentPeriodStartAt,
    DateTimeOffset? CurrentPeriodEndAt);
