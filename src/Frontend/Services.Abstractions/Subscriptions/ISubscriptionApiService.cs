using Contracts.DTOs;
using Contracts.Requests.Subscriptions;

namespace Services.Abstractions.Subscriptions;

public interface ISubscriptionApiService
{
    Task<EntitlementDto> GetEntitlementsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<IReadOnlyList<PlanCardDto>> GetPlansAsync(Guid workspaceId, CancellationToken ct = default);

    Task<SubscriptionDetailsDto> GetSubscriptionAsync(Guid workspaceId, CancellationToken ct = default);

    Task<CheckoutSessionResultDto> CreateCheckoutSessionAsync(Guid workspaceId, CreateCheckoutSessionRequest request,
        CancellationToken ct = default);

    Task<PortalSessionResultDto> CreatePortalSessionAsync(Guid workspaceId, CancellationToken ct = default);
}
