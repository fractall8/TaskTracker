using Contracts.DTOs;
using Contracts.Requests.Subscriptions;
using Services.Abstractions.Subscriptions;
using Services.Api;
using Services.Extensions;

namespace Services.Subscriptions;

public class SubscriptionApiService(ISubscriptionsApi subscriptionsApi) : ISubscriptionApiService
{
    public async Task<EntitlementDto> GetEntitlementsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await subscriptionsApi.GetEntitlementsAsync(workspaceId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<IReadOnlyList<PlanCardDto>> GetPlansAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await subscriptionsApi.GetPlansAsync(workspaceId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<SubscriptionDetailsDto> GetSubscriptionAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await subscriptionsApi.GetSubscriptionAsync(workspaceId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<CheckoutSessionResultDto> CreateCheckoutSessionAsync(Guid workspaceId,
        CreateCheckoutSessionRequest request, CancellationToken ct = default)
    {
        var response = await subscriptionsApi.CreateCheckoutSessionAsync(workspaceId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<PortalSessionResultDto> CreatePortalSessionAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await subscriptionsApi.CreatePortalSessionAsync(workspaceId, ct);
        return await response.HandleResponseAsync();
    }
}
