using Application.Interfaces.Repositories;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Subscriptions.Queries;

public record GetWorkspaceSubscriptionQuery(Guid WorkspaceId) : IRequest<SubscriptionDetailsDto>;

public class GetWorkspaceSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetWorkspaceSubscriptionQuery, SubscriptionDetailsDto>
{
    public async Task<SubscriptionDetailsDto> Handle(
        GetWorkspaceSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(
            request.WorkspaceId,
            cancellationToken);

        if (subscription is null)
        {
            return new SubscriptionDetailsDto(
                PlanId: "free",
                HasBillableSubscription: false,
                Status: null,
                CurrentPeriodStartAt: null,
                CurrentPeriodEndAt: null,
                CancelAtPeriodEnd: false
            );
        }

        return new SubscriptionDetailsDto(
            PlanId: subscription.PlanId ?? "free",
            HasBillableSubscription: true,
            Status: subscription.Status,
            CurrentPeriodStartAt: subscription.CurrentPeriodStartAt,
            CurrentPeriodEndAt: subscription.CurrentPeriodEndAt,
            CancelAtPeriodEnd: subscription.CancelAtPeriodEnd
        );
    }
}
