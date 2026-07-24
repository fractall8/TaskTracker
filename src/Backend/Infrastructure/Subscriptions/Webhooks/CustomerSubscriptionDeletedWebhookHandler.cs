using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;

namespace Infrastructure.Subscriptions.Webhooks;

public sealed class CustomerSubscriptionDeletedWebhookHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork)
    : ISubscriptionWebhookEventHandler
{
    public string EventType => StripeWebhookEventTypes.CustomerSubscriptionDeleted;

    public async Task HandleAsync(SubscriptionWebhookEventDto subscriptionWebhookEventDto,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionWebhookEventDto.StripeSubscriptionId))
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

        subscription.Status = SubscriptionStatus.Canceled;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
