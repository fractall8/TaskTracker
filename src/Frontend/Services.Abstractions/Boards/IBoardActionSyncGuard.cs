using Contracts.Notifications.BoardActions;

namespace Services.Abstractions.Boards;

public interface IBoardActionSyncGuard
{
    bool TryAccept(BoardActionNotification notification, Guid? currentBoardId, Guid currentUserId);

    void MarkApplied(BoardActionNotification notification);

    void Reset();
}
