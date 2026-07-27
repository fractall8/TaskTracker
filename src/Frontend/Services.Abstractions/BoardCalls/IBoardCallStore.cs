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

    // Fired specifically when a CallEnded notification for the currently-tracked ActiveCall is applied
    // — i.e. someone else ended a call this client's own AcsCallInteropService may currently be
    // connected to. Narrower than StateChanged (which fires for many unrelated reasons) so a listener
    // can react precisely (force a local ACS hangup) without over-triggering.
    event Action<Guid>? CallEndedRemotely;

    void Reset();

    Task LoadActiveCallAsync(Guid boardId, CancellationToken ct = default);

    Task<StartOrJoinBoardCallResponse> StartCallAsync(Guid boardId, CancellationToken ct = default);

    Task<StartOrJoinBoardCallResponse> JoinCallAsync(Guid boardId, CancellationToken ct = default);

    Task LeaveCallAsync(Guid boardId, CancellationToken ct = default);

    Task EndCallAsync(Guid boardId, CancellationToken ct = default);

    void ApplyAction(BoardActionNotification notification, Guid currentUserId);
}
