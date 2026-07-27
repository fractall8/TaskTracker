using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Services.Abstractions.Auth;
using Services.Abstractions.BoardCalls;
using Services.Abstractions.Boards;

namespace Services.BoardCalls.Stores;

public class BoardCallStore(
    IBoardCallApiService boardCallApi,
    IBoardActionSyncGuard syncGuard,
    IProfileStore profileStore) : IBoardCallStore
{
    public Guid? BoardId { get; private set; }

    public BoardCallDto? ActiveCall { get; private set; }

    public bool IsLoading { get; private set; }

    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public event Action<Guid>? CallEndedRemotely;

    // Bumped by every LoadActiveCallAsync call and every successfully-applied real-time notification.
    // Lets an in-flight GET recognize, once it resolves, that something fresher already landed while it
    // was in the air (a newer load, or a CallStarted/CallEnded/CallParticipantsChanged push) — so it can
    // discard its now-stale response instead of clobbering the fresher state.
    private int _generation;

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public void Reset()
    {
        _generation++;
        BoardId = null;
        ActiveCall = null;
        IsLoading = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    public async Task LoadActiveCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var generation = ++_generation;

        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            BoardId = boardId;
            ActiveCall = null;
            var call = await boardCallApi.GetActiveCallAsync(boardId, ct);

            if (generation == _generation)
            {
                ActiveCall = call;
            }
        }
        catch (Exception ex)
        {
            if (generation == _generation)
            {
                ErrorMessage = ex.Message;
            }
        }
        finally
        {
            if (generation == _generation)
            {
                IsLoading = false;
            }

            NotifyStateChanged();
        }
    }

    public async Task<StartOrJoinBoardCallResponse> StartCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallApi.StartCallAsync(boardId, ct);

        // Guards against a concurrent Reset()/LoadActiveCallAsync for a different board completing while
        // this request was in flight (e.g. the user navigated away mid-call-setup) — a stale response for
        // a board this store no longer represents must be silently dropped, not applied.
        if (BoardId == boardId)
        {
            _generation++;
            ActiveCall = response.Call;
            NotifyStateChanged();
        }

        return response;
    }

    public async Task<StartOrJoinBoardCallResponse> JoinCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallApi.JoinCallAsync(boardId, ct);

        if (BoardId == boardId)
        {
            _generation++;
            ActiveCall = response.Call;
            NotifyStateChanged();
        }

        return response;
    }

    public async Task LeaveCallAsync(Guid boardId, CancellationToken ct = default)
    {
        // boardId is taken explicitly from the caller (which always knows it unambiguously — the call
        // page's own route parameter) rather than read from the ambient BoardId property: a concurrent
        // navigation elsewhere (e.g. Board.razor's own OnParametersSetAsync calling Reset()) can null out
        // BoardId while this leave/end is still in flight, which previously made this throw "Board is not
        // loaded" and abandon the call server-side without ever actually ending it.
        await boardCallApi.LeaveCallAsync(boardId, ct);

        if (BoardId == boardId)
        {
            _generation++;

            // The CallParticipantsChanged notification this triggers is filtered out for the leaver's own
            // client by BoardActionsSyncGuard (it never re-delivers a user's own actions back to them), so
            // without this the leaver's local participant count would stay stale until someone else's action
            // happens to refresh it. Apply the same removal locally instead of waiting for a notification.
            if (ActiveCall is { } activeCall && profileStore.Profile is { } profile)
            {
                ActiveCall = activeCall with
                {
                    Participants = activeCall.Participants.Where(p => p.UserId != profile.Id).ToList()
                };
            }

            NotifyStateChanged();
        }
    }

    public async Task EndCallAsync(Guid boardId, CancellationToken ct = default)
    {
        await boardCallApi.EndCallAsync(boardId, ct);

        if (BoardId == boardId)
        {
            _generation++;
            ActiveCall = null;
            NotifyStateChanged();
        }
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
            _generation++;
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

        // BoardActionsSyncGuard already filtered out the actor's own action, so this only ever fires for
        // OTHER participants — exactly who needs to be told to hang up their own live ACS session too.
        CallEndedRemotely?.Invoke(payload.BoardCallId);

        return true;
    }
}
