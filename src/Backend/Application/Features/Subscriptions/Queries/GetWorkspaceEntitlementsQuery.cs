using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Subscriptions.Queries;

public record GetWorkspaceEntitlementsQuery(Guid WorkspaceId) : IRequest<EntitlementDto>;

public class GetWorkspaceEntitlementsQueryHandler(
    ISubscriptionRepository subscriptionRepository,
    IPlanCatalog planCatalog)
    : IRequestHandler<GetWorkspaceEntitlementsQuery, EntitlementDto>
{
    public async Task<EntitlementDto> Handle(
        GetWorkspaceEntitlementsQuery request,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(
            request.WorkspaceId,
            cancellationToken);

        var planId = subscription?.PlanId ?? "free";
        var planConfig = planCatalog.GetPlan(planId);

        return new EntitlementDto(
            PlanId: planId,
            PlanDisplayName: planConfig.PlanDisplayName,
            Status: subscription?.Status ?? "free",
            Features: planConfig.Features
        );
    }
}
