using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.Subscriptions.Commands;

public record CreateCustomerPortalSessionCommand(Guid WorkspaceId) : IRequest<string>;

public class CreateCustomerPortalSessionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionService subscriptionService,
    IWorkspaceAccessService workspaceAccessService,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository)
    : IRequestHandler<CreateCustomerPortalSessionCommand, string>
{
    public async Task<string> Handle(CreateCustomerPortalSessionCommand request, CancellationToken ct)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u, ct);

        if (userInfo is null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        await workspaceAccessService.EnsureCanManageSubscriptionsMembersAsync(userInfo.Id, ct);

        var subscription = await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(request.WorkspaceId, ct);

        if (subscription is null || string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
        {
            throw new InvalidOperationException("This workspace does not have an active billing profile.");
        }

        var portalUrl = await subscriptionService.CreateCustomerPortalSessionAsync(
            subscription.StripeCustomerId,
            ct);

        return portalUrl;
    }
}
