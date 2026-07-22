using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;

namespace Infrastructure.Subscriptions.Webhooks;

public sealed class CustomerSubscriptionUpdatedWebhookHandler(
    ISubscriptionRepository subscriptionRepository)
    : ISubscriptionWebhookEventHandler
{
    public string EventType => StripeWebhookEventTypes.CustomerSubscriptionUpdated;

    public async Task HandleAsync(SubscriptionWebhookEventDto subscriptionWebhookEventDto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionWebhookEventDto.StripeSubscriptionId) || string.IsNullOrWhiteSpace(subscriptionWebhookEventDto.Status))
        {
            throw new InvalidOperationException("Subscription webhook payload is invalid.");
        }

        var subscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(
            subscriptionWebhookEventDto.StripeSubscriptionId,
            ct);

        if (subscription is null)
        {
            return;
        }

        if (!subscription.Status.Equals(subscriptionWebhookEventDto.Status, StringComparison.Ordinal))
        {
            if (!SubscriptionStatus.IsDocumentedStatus(subscriptionWebhookEventDto.Status))
            {
                throw new KeyNotFoundException($"Subscription webhook status {subscriptionWebhookEventDto.Status} is not supported.");
            }

            subscription.Status = subscriptionWebhookEventDto.Status;
        }

        if (subscription.CancelAtPeriodEnd != subscriptionWebhookEventDto.CancelAtPeriodEnd)
        {
            subscription.CancelAtPeriodEnd = subscriptionWebhookEventDto.CancelAtPeriodEnd;
        }

        if (subscriptionWebhookEventDto.CurrentPeriodStartAt is { } periodStart && subscription.CurrentPeriodStartAt != periodStart)
        {
            subscription.CurrentPeriodStartAt = periodStart;
        }

        if (subscriptionWebhookEventDto.CurrentPeriodEndAt is { } periodEnd && subscription.CurrentPeriodEndAt != periodEnd)
        {
            subscription.CurrentPeriodEndAt = periodEnd;
        }
    }
}
