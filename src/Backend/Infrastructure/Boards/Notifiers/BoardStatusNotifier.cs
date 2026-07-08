using Application.Interfaces.Notifiers;
using Contracts.Notifications;
using Infrastructure.Boards.Hubs;
using Infrastructure.Common.Constants;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Boards.Notifiers;


internal class BoardExportStatusNotifier(IHubContext<BoardExportStatusHub> hubContext) : IBoardExportStatusNotifier
{
    public Task NotifyExportStatusChangedAsync(BoardExportStatusChangedNotification notification, CancellationToken ct = default) =>
        SendAsync(BoardExportHubEvents.ExportStatusChanged, notification, ct);

    public Task NotifyReExportStatusChangedAsync(BoardExportStatusChangedNotification notification, CancellationToken ct = default) =>
        SendAsync(BoardExportHubEvents.ReExportStatusChanged, notification, ct);

    private Task SendAsync(string eventName, BoardExportStatusChangedNotification notification, CancellationToken ct) =>
        hubContext.Clients
            .Group(HubGroupNames.BoardExportStatus.Get(notification.BoardId))
            .SendAsync(eventName, notification, ct);
}
