using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Subscriptions.Queries;

public record GetSubscriptionPlansQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<PlanCardDto>>;

public class GetSubscriptionPlansQueryHandler(
    IPlanCatalog planCatalog,
    ISubscriptionService subscriptionService)
    : IRequestHandler<GetSubscriptionPlansQuery, IReadOnlyList<PlanCardDto>>
{
    public async Task<IReadOnlyList<PlanCardDto>> Handle(
        GetSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = planCatalog.GetAllPlans();
        var planCards = new List<PlanCardDto>(plans.Count);

        foreach (var plan in plans)
        {
            PlanPriceDto? priceDto = null;

            var stripePriceId = planCatalog.TryGetPriceId(plan.PlanId);

            if (!string.IsNullOrWhiteSpace(stripePriceId))
            {
                priceDto = await subscriptionService.GetPriceAsync(stripePriceId, cancellationToken);
            }

            planCards.Add(new PlanCardDto(
                PlanId: plan.PlanId,
                PlanDisplayName: plan.PlanDisplayName,
                Price: priceDto,
                Features: plan.Features
            ));
        }

        return planCards;
    }
}
