using Contracts.Notifications;

namespace Application.Interfaces.Notifiers;

public interface IBoardExportStatusNotifier
{
    Task NotifyExportStatusChangedAsync(BoardExportStatusChangedNotification notification, CancellationToken ct = default);

    Task NotifyReExportStatusChangedAsync(BoardExportStatusChangedNotification notification, CancellationToken ct = default);
}
