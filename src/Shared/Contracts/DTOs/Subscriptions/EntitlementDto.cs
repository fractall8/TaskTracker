namespace Contracts.DTOs;

public record EntitlementDto(
    string PlanId,
    string PlanDisplayName,
    string Status,
    IReadOnlyList<string> Features,
    SubscriptionLimitsDto Limits,
    SubscriptionUsageDto Usage);
