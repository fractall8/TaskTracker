using Contracts.Notifications.BoardActions;
using Services.Abstractions.Boards;

namespace Services.Boards;

public sealed class BoardActionSyncGuard : IBoardActionSyncGuard
{
    private readonly Dictionary<string, DateTimeOffset> _lastAppliedAt = [];

    public bool TryAccept(BoardActionNotification notification, Guid? currentBoardId, Guid currentUserId)
    {
        if (currentBoardId is not { } boardId || boardId != notification.BoardId)
        {
            return false;
        }

        if (currentUserId != Guid.Empty && notification.ActorUserId == currentUserId)
        {
            return false;
        }

        var syncKey = BoardActionSyncKey.Resolve(notification);
        if (_lastAppliedAt.TryGetValue(syncKey, out var lastApplied) && notification.OccurredAt <= lastApplied)
        {
            return false;
        }

        return true;
    }

    public void MarkApplied(BoardActionNotification notification)
    {
        var syncKey = BoardActionSyncKey.Resolve(notification);
        _lastAppliedAt[syncKey] = notification.OccurredAt;
    }

    public void Reset() => _lastAppliedAt.Clear();
}
