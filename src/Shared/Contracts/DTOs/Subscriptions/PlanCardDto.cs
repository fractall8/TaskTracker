namespace Contracts.DTOs;

public record PlanCardDto(
    string PlanId,
    string PlanDisplayName,
    IReadOnlyList<string> Features,
    PlanPriceDto? Price,
    SubscriptionLimitsDto Limits);
