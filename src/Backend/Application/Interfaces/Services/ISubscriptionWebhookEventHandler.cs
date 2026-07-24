using Contracts.DTOs;

namespace Application.Interfaces.Services;

public interface ISubscriptionWebhookEventHandler
{
    string EventType { get; }

    Task HandleAsync(SubscriptionWebhookEventDto subscriptionEvent, CancellationToken ct = default);
}
