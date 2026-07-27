using System.Text.Json;
using Application.Features.BoardCalls.Commands;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Route("internal/board-call-events")]
[ApiController]
public class BoardCallEventsController(ISender sender, ILogger<BoardCallEventsController> logger) : ControllerBase
{
    private const string _subscriptionValidationEventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
    private const string _callParticipantAddedEventType = "Microsoft.Communication.CallParticipantAdded";
    private const string _callParticipantRemovedEventType = "Microsoft.Communication.CallParticipantRemoved";

    private static readonly JsonSerializerOptions _eventDataJsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(CancellationToken ct)
    {
        var requestBody = await BinaryData.FromStreamAsync(Request.Body, ct);
        EventGridEvent[] events;

        try
        {
            events = EventGridEvent.ParseMany(requestBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Received a malformed Event Grid request body");
            return BadRequest();
        }

        foreach (var evt in events)
        {
            if (evt.EventType == _subscriptionValidationEventType)
            {
                var validationData = evt.Data.ToObjectFromJson<SubscriptionValidationEventData>(_eventDataJsonOptions);

                if (validationData?.ValidationCode is { } validationCode)
                {
                    return Ok(new SubscriptionValidationAck(validationCode));
                }

                return Ok();
            }
        }

        foreach (var evt in events)
        {
            switch (evt.EventType)
            {
                case _callParticipantAddedEventType:
                {
                    if (TryParseParticipantEvent(evt, out var roomId, out var rawId))
                    {
                        await sender.Send(new RecordCallParticipantJoinedCommand(roomId, rawId, evt.EventTime), ct);
                    }

                    break;
                }

                case _callParticipantRemovedEventType:
                {
                    if (TryParseParticipantEvent(evt, out var roomId, out var rawId))
                    {
                        await sender.Send(new RecordCallParticipantLeftCommand(roomId, rawId, evt.EventTime), ct);
                    }

                    break;
                }

                default:
                    logger.LogDebug("Ignoring unhandled Event Grid event type {EventType}", evt.EventType);
                    break;
            }
        }

        return Ok();
    }

    // Isolated per-event so one malformed event's payload can't throw and abort the whole batch —
    // otherwise Event Grid would retry the entire delivery, including events already processed above.
    private bool TryParseParticipantEvent(EventGridEvent evt, out string roomId, out string rawId)
    {
        roomId = string.Empty;
        rawId = string.Empty;

        BoardCallParticipantEventData? data;

        try
        {
            data = evt.Data.ToObjectFromJson<BoardCallParticipantEventData>(_eventDataJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed {EventType} event payload, skipping", evt.EventType);
            return false;
        }

        if (data?.Room?.Id is { } parsedRoomId && data.User?.CommunicationIdentifier?.RawId is { } parsedRawId)
        {
            roomId = parsedRoomId;
            rawId = parsedRawId;
            return true;
        }

        logger.LogWarning("Malformed {EventType} event, missing room or user id", evt.EventType);
        return false;
    }
}

internal sealed record SubscriptionValidationAck(string ValidationResponse);

internal sealed record BoardCallParticipantEventData(
    BoardCallParticipantEventUser? User,
    BoardCallParticipantEventRoom? Room);

internal sealed record BoardCallParticipantEventUser(BoardCallParticipantEventIdentifier? CommunicationIdentifier);

internal sealed record BoardCallParticipantEventIdentifier(string? RawId);

internal sealed record BoardCallParticipantEventRoom(string? Id);
