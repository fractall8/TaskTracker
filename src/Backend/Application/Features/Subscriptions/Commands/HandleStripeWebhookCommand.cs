using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Subscriptions.Commands;

public record HandleStripeWebhookCommand(string Payload, string StripeSignature) : IRequest;

public class HandleStripeWebhookCommandHandler(
    ISubscriptionService subscriptionService,
    IEnumerable<ISubscriptionWebhookEventHandler> eventHandlers,
    IStripeWebhookEventRepository webhookEventRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<HandleStripeWebhookCommandHandler> logger)
    : IRequestHandler<HandleStripeWebhookCommand>
{
    public async Task Handle(HandleStripeWebhookCommand request, CancellationToken ct)
    {
        SubscriptionWebhookEventDto subscriptionWebhookEventDto;

        try
        {
            subscriptionWebhookEventDto = await subscriptionService.ParseWebhookEventAsync(
                request.Payload,
                request.StripeSignature,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rejected Stripe webhook due to invalid signature or payload.");
            throw new ArgumentException("Invalid Stripe signature or payload.", ex);
        }

        var webhookEvent = await webhookEventRepository.GetByEventIdAsync(subscriptionWebhookEventDto.EventId, ct);

        if (webhookEvent is { ProcessedAt: not null })
        {
            return;
        }

        webhookEvent ??= await RegisterNewEventAsync(subscriptionWebhookEventDto);

        var handler = eventHandlers.FirstOrDefault(h =>
            h.EventType.Equals(subscriptionWebhookEventDto.EventType, StringComparison.Ordinal));

        if (handler is null)
        {
            logger.LogInformation(
                "No handler registered for Stripe event type '{EventType}'. Acknowledging event {EventId}.",
                subscriptionWebhookEventDto.EventType,
                subscriptionWebhookEventDto.EventId);

            MarkProcessed(webhookEvent);
            await unitOfWork.SaveChangesAsync(ct);

            return;
        }

        await handler.HandleAsync(subscriptionWebhookEventDto, ct);

        MarkProcessed(webhookEvent);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<StripeWebhookEvent> RegisterNewEventAsync(SubscriptionWebhookEventDto subscriptionWebhookEventDto)
    {
        var webhookEvent = new StripeWebhookEvent
        {
            EventId = subscriptionWebhookEventDto.EventId,
            EventType = subscriptionWebhookEventDto.EventType,
            ReceivedAt = dateTimeProvider.UtcNow,
        };

        await webhookEventRepository.AddAsync(webhookEvent);

        return webhookEvent;
    }

    private void MarkProcessed(StripeWebhookEvent webhookEvent)
    {
        webhookEvent.ProcessedAt = dateTimeProvider.UtcNow;
    }
}

public class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .Must(payload => !string.IsNullOrWhiteSpace(payload))
            .WithMessage("'Payload' must not be empty.");

        RuleFor(x => x.StripeSignature)
            .Must(signature => !string.IsNullOrWhiteSpace(signature))
            .WithMessage("'StripeSignature' must not be empty.");
    }
}
