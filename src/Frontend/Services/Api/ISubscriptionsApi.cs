using Contracts.DTOs;
using Contracts.Requests.Subscriptions;
using Refit;

namespace Services.Api;

public interface ISubscriptionsApi
{
    [Get("/api/workspaces/{workspaceId}/subscriptions/entitlements")]
    Task<IApiResponse<EntitlementDto>> GetEntitlementsAsync(Guid workspaceId, CancellationToken ct = default);

    [Get("/api/workspaces/{workspaceId}/subscriptions/plans")]
    Task<IApiResponse<IReadOnlyList<PlanCardDto>>> GetPlansAsync(Guid workspaceId, CancellationToken ct = default);

    [Get("/api/workspaces/{workspaceId}/subscriptions/subscription")]
    Task<IApiResponse<SubscriptionDetailsDto>> GetSubscriptionAsync(Guid workspaceId, CancellationToken ct = default);

    [Post("/api/workspaces/{workspaceId}/subscriptions/checkout")]
    Task<IApiResponse<CheckoutSessionResultDto>> CreateCheckoutSessionAsync(Guid workspaceId,
        [Body] CreateCheckoutSessionRequest request, CancellationToken ct = default);

    [Post("/api/workspaces/{workspaceId}/subscriptions/portal")]
    Task<IApiResponse<PortalSessionResultDto>> CreatePortalSessionAsync(Guid workspaceId,
        CancellationToken ct = default);
}
