using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Services.Abstractions.BoardCalls;
using Services.Abstractions.Boards;

namespace Services.BoardCalls.Stores;

public class BoardCallStore(
    IBoardCallApiService boardCallApi,
    IBoardActionSyncGuard syncGuard) : IBoardCallStore
{
    public Guid? BoardId { get; private set; }

    public BoardCallDto? ActiveCall { get; private set; }

    public bool IsLoading { get; private set; }

    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public void Reset()
    {
        BoardId = null;
        ActiveCall = null;
        IsLoading = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    public async Task LoadActiveCallAsync(Guid boardId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            BoardId = boardId;
            ActiveCall = await boardCallApi.GetActiveCallAsync(boardId, ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<StartOrJoinBoardCallResponse> StartCallAsync(CancellationToken ct = default)
    {
        if (BoardId is null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        var response = await boardCallApi.StartCallAsync(BoardId.Value, ct);

        ActiveCall = response.Call;
        NotifyStateChanged();

        return response;
    }

    public async Task<StartOrJoinBoardCallResponse> JoinCallAsync(CancellationToken ct = default)
    {
        if (BoardId is null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        var response = await boardCallApi.JoinCallAsync(BoardId.Value, ct);

        ActiveCall = response.Call;
        NotifyStateChanged();

        return response;
    }

    public async Task LeaveCallAsync(CancellationToken ct = default)
    {
        if (BoardId is null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        await boardCallApi.LeaveCallAsync(BoardId.Value, ct);
    }

    public async Task EndCallAsync(CancellationToken ct = default)
    {
        if (BoardId is null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        await boardCallApi.EndCallAsync(BoardId.Value, ct);

        ActiveCall = null;
        NotifyStateChanged();
    }

    public void ApplyAction(BoardActionNotification notification, Guid currentUserId)
    {
        if (!syncGuard.TryAccept(notification, BoardId, currentUserId))
        {
            return;
        }

        bool applied = notification.Type switch
        {
            BoardActionNotificationType.CallStarted => ApplyCallStarted((CallStartedPayload)notification.Payload),
            BoardActionNotificationType.CallParticipantsChanged => ApplyCallParticipantsChanged((CallParticipantsChangedPayload)notification.Payload),
            BoardActionNotificationType.CallEnded => ApplyCallEnded((CallEndedPayload)notification.Payload),
            _ => false
        };

        if (applied)
        {
            syncGuard.MarkApplied(notification);
            NotifyStateChanged();
        }
    }

    private bool ApplyCallStarted(CallStartedPayload payload)
    {
        ActiveCall = payload.Call;
        return true;
    }

    private bool ApplyCallParticipantsChanged(CallParticipantsChangedPayload payload)
    {
        if (ActiveCall is null || ActiveCall.Id != payload.BoardCallId)
        {
            return false;
        }

        ActiveCall = ActiveCall with { Participants = payload.Participants };
        return true;
    }

    private bool ApplyCallEnded(CallEndedPayload payload)
    {
        if (ActiveCall is null || ActiveCall.Id != payload.BoardCallId)
        {
            return false;
        }

        ActiveCall = null;
        return true;
    }
}
