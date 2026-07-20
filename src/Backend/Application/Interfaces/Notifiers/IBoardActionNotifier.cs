using Contracts.Notifications.BoardActions;

namespace Application.Interfaces.Notifiers;

public interface IBoardActionNotifier
{
    Task NotifyAsync(BoardActionNotification notification, CancellationToken ct);
}
