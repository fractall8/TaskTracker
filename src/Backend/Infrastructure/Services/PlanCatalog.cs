using Application.Interfaces.Services;
using Contracts.DTOs;
using Infrastructure.Subscriptions.Options;

namespace Infrastructure.Services;

internal class PlanCatalog(SubscriptionOptions subscriptionOptions) : IPlanCatalog
{
    public string DefaultPlanId => subscriptionOptions.DefaultPlanId;

    public PlanDto GetPlan(string planId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);

        if (subscriptionOptions.Plans is null
            || !subscriptionOptions.Plans.TryGetValue(planId, out var plan))
        {
            throw new InvalidOperationException($"Billing plan '{planId}' is not defined in configuration.");
        }

        return ToPlanDto(plan);
    }

    public IReadOnlyList<PlanDto> GetAllPlans()
    {
        if (subscriptionOptions.Plans is null || subscriptionOptions.Plans.Count == 0)
        {
            return [];
        }

        return subscriptionOptions.Plans
            .Values
            .Select(ToPlanDto)
            .ToList();
    }

    public string? TryGetPriceId(string planId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);

        if (subscriptionOptions.Plans is null
            || !subscriptionOptions.Plans.TryGetValue(planId, out var plan))
        {
            throw new InvalidOperationException($"Billing plan '{planId}' is not defined in configuration.");
        }

        return string.IsNullOrWhiteSpace(plan.PriceId) ? null : plan.PriceId;
    }

    public string GetPriceId(string planId)
    {
        var priceId = TryGetPriceId(planId);

        if (priceId is null)
        {
            throw new InvalidOperationException(
                $"Billing plan '{planId}' does not have a Stripe price configured.");
        }

        return priceId;
    }

    private static PlanDto ToPlanDto(PlanOptions plan) =>
        new(plan.Id, plan.DisplayName, plan.Features);
}
