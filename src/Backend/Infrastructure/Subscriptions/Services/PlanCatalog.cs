using Application.Interfaces.Services;
using Contracts.DTOs;
using Infrastructure.Subscriptions.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Subscriptions.Services;

internal class PlanCatalog(IOptions<SubscriptionOptions> options) : IPlanCatalog
{
    private readonly SubscriptionOptions _subscriptionOptions = options.Value;
    public string DefaultPlanId => _subscriptionOptions.DefaultPlanId;

    public PlanDto GetPlan(string planId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);

        if (_subscriptionOptions.Plans is null
            || !_subscriptionOptions.Plans.TryGetValue(planId, out var plan))
        {
            throw new InvalidOperationException($"Billing plan '{planId}' is not defined in configuration.");
        }

        return ToPlanDto(plan);
    }

    public IReadOnlyList<PlanDto> GetAllPlans()
    {
        if (_subscriptionOptions.Plans is null || _subscriptionOptions.Plans.Count == 0)
        {
            return [];
        }

        return _subscriptionOptions.Plans
            .Values
            .Select(ToPlanDto)
            .ToList();
    }

    public string? TryGetPriceId(string planId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);

        if (_subscriptionOptions.Plans is null
            || !_subscriptionOptions.Plans.TryGetValue(planId, out var plan))
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
