using System.Security.Claims;
using Application.Features.Hubs.Commands;
using Infrastructure.Auth.Constants;
using Infrastructure.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Boards.Hubs;

public static class BoardActionsHubEvents
{
    public const string BoardChanged = "BoardChanged";
}

public class BoardActionsHub(
    ISender sender,
    ILogger<BoardExportStatusHub> logger) : Hub
{
    public async Task SubscribeAsync(Guid boardId)
    {
        try
        {
            var ct = Context.ConnectionAborted;

            var claimValue = GetClaim(Context.User, EntraClaimTypes.ObjectId)
                             ?? GetClaim(Context.User, ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(claimValue) || !Guid.TryParse(claimValue, out var objectId))
            {
                throw new HubException("Unauthorized: Invalid or missing user identity.");
            }

            await sender.Send(new SubscribeBoardActionsCommand(boardId, objectId), ct);

            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.BoardActions.Get(boardId), ct);

            logger.LogDebug(
                "Subscribed connection {ConnectionId} to BoardActions group.",
                Context.ConnectionId);
        }
        catch (Exception e)
        {
            throw new HubException(e.Message);
        }
    }

    public async Task UnsubscribeAsync(Guid boardId)
    {
        var ct = Context.ConnectionAborted;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroupNames.BoardActions.Get(boardId), ct);

        logger.LogDebug(
            "Unsubscribed connection {ConnectionId} from BoardActions group.",
            Context.ConnectionId);
    }

    public static string GetGroupName(Guid boardId) => $"Board_{boardId}_Export";

    private static string? GetClaim(ClaimsPrincipal? user, string type) =>
        user?.FindFirst(type)?.Value;
}
