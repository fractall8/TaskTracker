using Contracts.DTOs;
using Contracts.Notifications.BoardActions;

namespace Services.Abstractions.BoardCalls;

public interface IBoardCallStore
{
    Guid? BoardId { get; }

    BoardCallDto? ActiveCall { get; }

    bool IsLoading { get; }

    string? ErrorMessage { get; }

    event Action? StateChanged;

    void Reset();

    Task LoadActiveCallAsync(Guid boardId, CancellationToken ct = default);

    Task<StartOrJoinBoardCallResponse> StartCallAsync(CancellationToken ct = default);

    Task<StartOrJoinBoardCallResponse> JoinCallAsync(CancellationToken ct = default);

    Task LeaveCallAsync(CancellationToken ct = default);

    Task EndCallAsync(CancellationToken ct = default);

    void ApplyAction(BoardActionNotification notification, Guid currentUserId);
}
