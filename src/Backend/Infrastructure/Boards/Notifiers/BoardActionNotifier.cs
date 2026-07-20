using Application.Interfaces.Notifiers;
using Contracts.Notifications.BoardActions;
using Infrastructure.Boards.Hubs;
using Infrastructure.Common.Constants;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Boards.Notifiers;

public class BoardActionNotifier(IHubContext<BoardActionsHub> hubContext) : IBoardActionNotifier
{
    public Task NotifyAsync(BoardActionNotification notification, CancellationToken ct) =>
        hubContext.Clients
            .Group(HubGroupNames.BoardActions.Get(notification.BoardId))
            .SendAsync(BoardActionsHubEvents.BoardChanged, notification, ct);
}
