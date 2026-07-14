using System.Security.Claims;
using Application.Features.Hubs.Commands;
using Infrastructure.Auth.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Boards.Hubs;

public static class BoardExportHubEvents
{
    public const string ExportStatusChanged = "BoardExportStatusChanged";

    public const string ReExportStatusChanged = "BoardReExportStatusChanged";
}

[Authorize]
public class BoardExportStatusHub(
    ISender sender,
    ILogger<BoardExportStatusHub> logger) : Hub
{
    public async Task SubscribeAsync(IReadOnlyList<Guid> boardIds)
    {
        var ct = Context.ConnectionAborted;

        try
        {
            var claimValue = GetClaim(Context.User, EntraClaimTypes.ObjectId)
                             ?? GetClaim(Context.User, ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(claimValue) || !Guid.TryParse(claimValue, out var objectId))
            {
                throw new HubException("Unauthorized: Invalid or missing user identity.");
            }

            var subscribableBoardIds = await sender.Send(new SubscribeBoardExportStatusCommand(boardIds, objectId), ct);

            await Task.WhenAll(subscribableBoardIds.Select(boardId =>
                Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(boardId), ct)));

            logger.LogDebug(
                "Subscribed connection {ConnectionId} to {BoardCount} board export groups.",
                Context.ConnectionId,
                subscribableBoardIds.Count);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task UnsubscribeAsync(IReadOnlyList<Guid> boardIds)
    {
        var ct = Context.ConnectionAborted;

        var distinctBoardIds = boardIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (distinctBoardIds.Count == 0)
        {
            return;
        }

        await Task.WhenAll(distinctBoardIds.Select(boardId =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(boardId), ct)));

        logger.LogDebug(
            "Unsubscribed connection {ConnectionId} from {BoardCount} board export groups.",
            Context.ConnectionId,
            distinctBoardIds.Count);
    }

    public static string GetGroupName(Guid boardId) => $"Board_{boardId}_Export";

    private static string? GetClaim(ClaimsPrincipal? user, string type) =>
        user?.FindFirst(type)?.Value;
}
