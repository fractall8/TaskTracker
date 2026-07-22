using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.Subscriptions.Commands;

public record CreateCheckoutSessionCommand(Guid WorkspaceId, string PriceId) : IRequest<string>;

public class CreateCheckoutSessionCommandHandler(
    ISubscriptionService subscriptionService,
    IWorkspaceAccessService workspaceAccessService,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository)
    : IRequestHandler<CreateCheckoutSessionCommand, string>
{
    public async Task<string> Handle(CreateCheckoutSessionCommand request, CancellationToken ct)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u, ct);

        if (userInfo == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        await workspaceAccessService.EnsureCanManageSubscriptionsMembersAsync(userInfo.Id, ct);

        var checkoutSessionResultDto = await subscriptionService.CreateCheckoutSessionAsync(
            request.WorkspaceId,
            userInfo.Id,
            userInfo.Email,
            request.PriceId,
            null,
            ct);

        return checkoutSessionResultDto.Url;
    }
}
