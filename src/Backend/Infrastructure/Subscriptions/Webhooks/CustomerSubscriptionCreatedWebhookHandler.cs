using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Subscriptions.Webhooks;

public sealed class CustomerSubscriptionCreatedWebhookHandler(
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerSubscriptionCreatedWebhookHandler> logger)
    : ISubscriptionWebhookEventHandler
{
    public string EventType => StripeWebhookEventTypes.CustomerSubscriptionCreated;

    public async Task HandleAsync(SubscriptionWebhookEventDto subscriptionWebhookEventDto,
        CancellationToken ct = default)
    {
        if (!TryGetRequiredFields(subscriptionWebhookEventDto, out var userId, out var workspaceId, out var planId,
                out var customerId, out var subscriptionId, out var status))
        {
            logger.LogError("Subscription webhook payload is missing required metadata fields. StripeSubscriptionId: {SubId}", subscriptionWebhookEventDto.StripeSubscriptionId);
            return;
        }

        if (!SubscriptionStatus.IsBillable(status))
        {
            logger.LogInformation("Ignoring non-billable subscription status '{Status}' for Stripe subscription {StripeSubscriptionId}.", status, subscriptionId);
            return;
        }

        if (await subscriptionRepository.ExistsByStripeSubscriptionIdAsync(subscriptionId, ct))
        {
            logger.LogInformation("Stripe subscription {StripeSubscriptionId} already exists. Ignoring duplicate webhook.", subscriptionId);
            return;
        }

        if (await userRepository.GetByIdAsync(userId, ct) is null)
        {
            logger.LogWarning("User {UserId} not found for subscription {StripeSubscriptionId}. Cannot process webhook.", userId, subscriptionId);
            return;
        }

        if (await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(workspaceId, ct) is not null)
        {
            logger.LogWarning("Workspace {WorkspaceId} already has an active subscription. Ignoring new subscription {StripeSubscriptionId}.", workspaceId, subscriptionId);
            return;
        }

        await subscriptionRepository.AddAsync(new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkspaceId = workspaceId,
            PlanId = planId,
            StripeCustomerId = customerId,
            StripeSubscriptionId = subscriptionId,
            Status = status,
            CurrentPeriodStartAt = subscriptionWebhookEventDto.CurrentPeriodStartAt,
            CurrentPeriodEndAt = subscriptionWebhookEventDto.CurrentPeriodEndAt,
            CancelAtPeriodEnd = subscriptionWebhookEventDto.CancelAtPeriodEnd,
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Successfully processed and saved subscription {StripeSubscriptionId} for Workspace {WorkspaceId}.", subscriptionId, workspaceId);
    }

    private static bool TryGetRequiredFields(
        SubscriptionWebhookEventDto subscriptionWebhookEventDto,
        out Guid userId,
        out Guid workspaceId,
        out string planId,
        out string customerId,
        out string subscriptionId,
        out string status)
    {
        userId = subscriptionWebhookEventDto.UserId ?? Guid.Empty;
        workspaceId = subscriptionWebhookEventDto.WorkspaceId ?? Guid.Empty;
        planId = subscriptionWebhookEventDto.PlanId ?? string.Empty;
        customerId = subscriptionWebhookEventDto.StripeCustomerId ?? string.Empty;
        subscriptionId = subscriptionWebhookEventDto.StripeSubscriptionId ?? string.Empty;
        status = subscriptionWebhookEventDto.Status ?? string.Empty;

        return subscriptionWebhookEventDto.UserId is not null
               && subscriptionWebhookEventDto.WorkspaceId is not null
               && !string.IsNullOrWhiteSpace(planId)
               && !string.IsNullOrWhiteSpace(customerId)
               && !string.IsNullOrWhiteSpace(subscriptionId)
               && !string.IsNullOrWhiteSpace(status);
    }
}
