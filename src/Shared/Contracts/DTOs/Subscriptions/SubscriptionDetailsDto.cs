namespace Contracts.DTOs;

public record SubscriptionDetailsDto(
    string PlanId,
    string? Status,
    bool CancelAtPeriodEnd,
    bool HasBillableSubscription,
    DateTimeOffset? CurrentPeriodStartAt,
    DateTimeOffset? CurrentPeriodEndAt);
