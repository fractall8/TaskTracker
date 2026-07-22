using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Subscriptions.Webhooks;

public sealed class CustomerSubscriptionCreatedWebhookHandler(
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    ILogger<CustomerSubscriptionCreatedWebhookHandler> logger)
    : ISubscriptionWebhookEventHandler
{
    public string EventType => StripeWebhookEventTypes.CustomerSubscriptionCreated;

    public async Task HandleAsync(SubscriptionWebhookEventDto subscriptionWebhookEventDto,
        CancellationToken ct = default)
    {
        if (!TryGetRequiredFields(subscriptionWebhookEventDto, out var userId, out var planId, out var customerId,
                out var subscriptionId, out var status))
        {
            throw new InvalidOperationException("Subscription webhook payload is invalid.");
        }

        if (!SubscriptionStatus.IsBillable(status))
        {
            logger.LogInformation(
                "Ignoring non-billable subscription status '{Status}' for Stripe subscription {StripeSubscriptionId}.",
                status,
                subscriptionId);

            return;
        }

        if (await subscriptionRepository.ExistsByStripeSubscriptionIdAsync(subscriptionId, ct))
        {
            return;
        }

        if (await userRepository.GetByIdAsync(userId, ct) is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (await subscriptionRepository.GetSubscriptionByUserIdAsync(userId, ct) is not null)
        {
            throw new InvalidOperationException("Subscription already exists.");
        }

        await subscriptionRepository.AddAsync(new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            StripeCustomerId = customerId,
            StripeSubscriptionId = subscriptionId,
            Status = status,
            CurrentPeriodStartAt = subscriptionWebhookEventDto.CurrentPeriodStartAt,
            CurrentPeriodEndAt = subscriptionWebhookEventDto.CurrentPeriodEndAt,
            CancelAtPeriodEnd = subscriptionWebhookEventDto.CancelAtPeriodEnd,
        });
    }

    private static bool TryGetRequiredFields(
        SubscriptionWebhookEventDto billingEvent,
        out Guid userId,
        out string planId,
        out string customerId,
        out string subscriptionId,
        out string status)
    {
        userId = billingEvent.UserId ?? Guid.Empty;
        planId = billingEvent.PlanId ?? string.Empty;
        customerId = billingEvent.StripeCustomerId ?? string.Empty;
        subscriptionId = billingEvent.StripeSubscriptionId ?? string.Empty;
        status = billingEvent.Status ?? string.Empty;

        return billingEvent.UserId is not null
               && !string.IsNullOrWhiteSpace(planId)
               && !string.IsNullOrWhiteSpace(customerId)
               && !string.IsNullOrWhiteSpace(subscriptionId)
               && !string.IsNullOrWhiteSpace(status);
    }
}
