using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using Infrastructure.Subscriptions.Options;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Subscriptions.Services;

public class StripeSubscriptionsService(
    IStripeClient stripeClient,
    IOptions<StripeOptions> stripeOptions,
    IPlanCatalog planCatalog) : ISubscriptionService
{
    private const string _metadataUserIdKey = "userId";
    private const string _metadataWorkspaceIdKey = "workspaceId";
    private const string _metadataPlanIdKey = "planId";

    public async Task<CheckoutSessionResultDto> CreateCheckoutSessionAsync(
        Guid workspaceId,
        Guid userId,
        string email,
        string planId,
        string? stripeCustomerId = null,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);

        var priceId = planCatalog.GetPriceId(planId);
        var workspaceIdValue = workspaceId.ToString("D");
        var userIdValue = userId.ToString("D");

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = $"{stripeOptions.Value.SuccessUrl}?planId={planId}",
            CancelUrl = stripeOptions.Value.CancelUrl,
            ClientReferenceId = userIdValue,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                [_metadataUserIdKey] = userIdValue,
                [_metadataWorkspaceIdKey] = workspaceIdValue,
                [_metadataPlanIdKey] = planId,
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    [_metadataUserIdKey] = userIdValue,
                    [_metadataWorkspaceIdKey] = workspaceIdValue,
                    [_metadataPlanIdKey] = planId,
                },
            },
        };

        if (!string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            options.Customer = stripeCustomerId;
        }
        else
        {
            options.CustomerEmail = email;
        }

        var sessionService = new SessionService(stripeClient);
        var session = await sessionService.CreateAsync(options, cancellationToken: ct);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Checkout Session was created without a URL.");
        }

        return new CheckoutSessionResultDto(session.Id, session.Url);
    }

    public async Task<string> CreateCustomerPortalSessionAsync(
        string stripeCustomerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeCustomerId);

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = stripeOptions.Value.CancelUrl,
        };

        var portalService = new Stripe.BillingPortal.SessionService(stripeClient);
        var session = await portalService.CreateAsync(options, cancellationToken: ct);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Customer Portal Session was created without a URL.");
        }

        return session.Url;
    }

    public async Task<PlanPriceDto> GetPriceAsync(string stripePriceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripePriceId);

        var priceService = new PriceService(stripeClient);
        var price = await priceService.GetAsync(stripePriceId, cancellationToken: ct);

        if (price.UnitAmount is null)
        {
            throw new InvalidOperationException($"Stripe price '{stripePriceId}' has no unit amount.");
        }

        if (price.Recurring is null || string.IsNullOrWhiteSpace(price.Recurring.Interval))
        {
            throw new InvalidOperationException($"Stripe price '{stripePriceId}' is not a recurring price.");
        }

        return new PlanPriceDto(
            price.Currency,
            price.UnitAmount.Value,
            price.Recurring.Interval);
    }

    public Task<SubscriptionWebhookEventDto> ParseWebhookEventAsync(
        string payload,
        string stripeSignatureHeader,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeSignatureHeader);
        ct.ThrowIfCancellationRequested();

        var stripeEvent = EventUtility.ConstructEvent(
            payload,
            stripeSignatureHeader,
            stripeOptions.Value.WebhookSecret);

        return Task.FromResult(MapWebhookEvent(stripeEvent));
    }

    private static SubscriptionWebhookEventDto MapWebhookEvent(Event stripeEvent)
    {
        return stripeEvent.Data.Object switch
        {
            Subscription subscription => MapFromSubscription(stripeEvent, subscription),
            Session checkoutSession => MapFromCheckoutSession(stripeEvent, checkoutSession),
            _ => new SubscriptionWebhookEventDto(
                stripeEvent.Id,
                stripeEvent.Type,
                StripeCustomerId: null,
                StripeSubscriptionId: null,
                StripePriceId: null,
                WorkspaceId: null,
                PlanId: null,
                Status: null,
                CurrentPeriodStartAt: null,
                CurrentPeriodEndAt: null,
                CancelAtPeriodEnd: false,
                UserId: null),
        };
    }

    private static SubscriptionWebhookEventDto MapFromSubscription(Event stripeEvent, Subscription subscription)
    {
        var items = subscription.Items?.Data ?? [];
        var priceId = items
            .Select(item => item.Price?.Id)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

        return new SubscriptionWebhookEventDto(
            stripeEvent.Id,
            stripeEvent.Type,
            subscription.CustomerId,
            subscription.Id,
            priceId,
            TryGetWorkspaceId(subscription.Metadata),
            TryGetMetadataValue(subscription.Metadata, _metadataPlanIdKey),
            subscription.Status,
            items.Select(item => item.CurrentPeriodStart).DefaultIfEmpty().Min(),
            items.Select(item => item.CurrentPeriodEnd).DefaultIfEmpty().Max(),
            ResolveCancelAtPeriodEnd(subscription),
            TryGetUserId(subscription.Metadata));
    }

    private static bool ResolveCancelAtPeriodEnd(Subscription subscription) =>
        subscription.CancelAtPeriodEnd
        || (subscription.CancelAt != default
            && string.Equals(subscription.Status, SubscriptionStatus.Active, StringComparison.Ordinal));

    private static SubscriptionWebhookEventDto MapFromCheckoutSession(Event stripeEvent, Session session)
    {
        return new SubscriptionWebhookEventDto(
            stripeEvent.Id,
            stripeEvent.Type,
            session.CustomerId,
            session.SubscriptionId,
            StripePriceId: null,
            TryGetWorkspaceId(session.Metadata),
            TryGetMetadataValue(session.Metadata, _metadataPlanIdKey),
            Status: session.Status,
            CurrentPeriodStartAt: null,
            CurrentPeriodEndAt: null,
            CancelAtPeriodEnd: false,
            TryGetUserId(session.Metadata) ?? TryParseGuid(session.ClientReferenceId));
    }

    private static Guid? TryGetUserId(IDictionary<string, string>? metadata)
    {
        var value = TryGetMetadataValue(metadata, _metadataUserIdKey);

        return TryParseGuid(value);
    }

    private static Guid? TryGetWorkspaceId(IDictionary<string, string>? metadata)
    {
        var value = TryGetMetadataValue(metadata, _metadataWorkspaceIdKey);
        return TryParseGuid(value);
    }

    private static string? TryGetMetadataValue(IDictionary<string, string>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value;
    }

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var userId) ? userId : null;
}
