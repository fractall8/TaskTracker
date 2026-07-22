namespace Contracts.DTOs;

public record PlanPriceDto(
    string Currency,
    long UnitAmount,
    string Interval);
