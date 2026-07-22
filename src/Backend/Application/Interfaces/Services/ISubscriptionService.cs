using Contracts.DTOs;

namespace Application.Interfaces.Services;

public interface ISubscriptionService
{
    Task<CheckoutSessionResultDto> CreateCheckoutSessionAsync(
        Guid workspaceId,
        Guid userId,
        string email,
        string planId,
        string? stripeCustomerId = null,
        CancellationToken ct = default);

    Task<string> CreateCustomerPortalSessionAsync(
        string stripeCustomerId,
        CancellationToken ct = default);

    Task<PlanPriceDto> GetPriceAsync(string stripePriceId, CancellationToken ct = default);

    Task<SubscriptionWebhookEventDto> ParseWebhookEventAsync(
        string payload,
        string stripeSignatureHeader,
        CancellationToken ct = default);
}
