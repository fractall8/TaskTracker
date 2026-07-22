namespace Contracts.DTOs;

public record PlanDto(
    string PlanId,
    string PlanDisplayName,
    IReadOnlyList<string> Features);
