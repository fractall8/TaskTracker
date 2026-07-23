using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Subscriptions.Commands;

public record CreateCustomerPortalSessionCommand(Guid WorkspaceId) : IRequest<PortalSessionResultDto>;

public class CreateCustomerPortalSessionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionService subscriptionService,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<CreateCustomerPortalSessionCommand, PortalSessionResultDto>
{
    public async Task<PortalSessionResultDto> Handle(CreateCustomerPortalSessionCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanManageSubscriptionsAsync(request.WorkspaceId, ct);

        var subscription = await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(request.WorkspaceId, ct);

        if (subscription is null || string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
        {
            throw new BusinessRuleValidationException("This workspace does not have an active billing profile.");
        }

        var portalUrl = await subscriptionService.CreateCustomerPortalSessionAsync(
            request.WorkspaceId,
            subscription.StripeCustomerId,
            ct);

        return new PortalSessionResultDto(portalUrl);
    }
}
