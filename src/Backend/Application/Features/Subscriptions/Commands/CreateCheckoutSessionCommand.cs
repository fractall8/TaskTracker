using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Subscriptions.Commands;

public record CreateCheckoutSessionCommand(Guid WorkspaceId, string PriceId) : IRequest<CheckoutSessionResultDto>;

public class CreateCheckoutSessionCommandHandler(
    ISubscriptionService subscriptionService,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<CreateCheckoutSessionCommand, CheckoutSessionResultDto>
{
    public async Task<CheckoutSessionResultDto> Handle(CreateCheckoutSessionCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanManageSubscriptionsAsync(request.WorkspaceId, ct);

        var userInfo = await workspaceAccessService.GetCurrentUserInfoAsync(ct);

        var checkoutSessionResultDto = await subscriptionService.CreateCheckoutSessionAsync(
            request.WorkspaceId,
            userInfo.UserId,
            userInfo.Email,
            request.PriceId,
            null,
            ct);

        return checkoutSessionResultDto;
    }
}
