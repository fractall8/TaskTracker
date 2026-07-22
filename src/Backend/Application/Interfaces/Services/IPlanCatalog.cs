using Contracts.DTOs;

namespace Application.Interfaces.Services;

public interface IPlanCatalog
{
    string DefaultPlanId { get; }

    PlanDto GetPlan(string planId);

    IReadOnlyList<PlanDto> GetAllPlans();

    string? TryGetPriceId(string planId);

    string GetPriceId(string planId);
}
