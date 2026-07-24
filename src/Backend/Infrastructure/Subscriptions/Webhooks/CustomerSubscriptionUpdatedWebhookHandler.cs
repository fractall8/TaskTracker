using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Subscriptions.Webhooks;

public sealed class CustomerSubscriptionUpdatedWebhookHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerSubscriptionUpdatedWebhookHandler> logger)
    : ISubscriptionWebhookEventHandler
{
    public string EventType => StripeWebhookEventTypes.CustomerSubscriptionUpdated;

    public async Task HandleAsync(SubscriptionWebhookEventDto subscriptionWebhookEventDto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionWebhookEventDto.StripeSubscriptionId) || string.IsNullOrWhiteSpace(subscriptionWebhookEventDto.Status))
        {
            logger.LogError("Subscription webhook payload is missing required metadata fields. StripeSubscriptionId: {SubId}", subscriptionWebhookEventDto.StripeSubscriptionId);
            return;
        }

        var subscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(
            subscriptionWebhookEventDto.StripeSubscriptionId,
            ct);

        if (subscription is null)
        {
            logger.LogWarning("Received update for Stripe subscription {SubId}, but it was not found in the database. Ignoring.", subscriptionWebhookEventDto.StripeSubscriptionId);
            return;
        }

        if (!subscription.Status.Equals(subscriptionWebhookEventDto.Status, StringComparison.Ordinal))
        {
            if (!SubscriptionStatus.IsDocumentedStatus(subscriptionWebhookEventDto.Status))
            {
                logger.LogError("Subscription webhook status {Status} is not supported.", subscriptionWebhookEventDto.Status);
                return;
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

        await unitOfWork.SaveChangesAsync(ct);
    }
}
