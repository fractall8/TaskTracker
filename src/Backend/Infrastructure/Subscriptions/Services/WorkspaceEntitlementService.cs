using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Infrastructure.Subscriptions.Services;

public class WorkspaceEntitlementService(
    ISubscriptionRepository subscriptionRepository,
    IPlanCatalog planCatalog) : IWorkspaceEntitlementService
{
    public async Task<bool> HasFeatureAsync(Guid workspaceId, string feature, CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(workspaceId, ct);

        var planId = subscription?.PlanId ?? planCatalog.DefaultPlanId;
        var plan = planCatalog.GetPlan(planId);

        return plan.Features.Contains(feature);
    }
}
