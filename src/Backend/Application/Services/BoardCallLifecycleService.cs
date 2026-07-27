using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;

namespace Application.Services;

public class BoardCallLifecycleService(
    IBoardCallRepository boardCallRepository,
    IAcsCallService acsCallService,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IBoardCallLifecycleService
{
    public async Task EndCallAsync(Guid boardCallId, Guid? endedByUserId = null, CancellationToken ct = default)
    {
        var call = await boardCallRepository.GetActiveCallWithParticipantsAsync(boardCallId, ct);

        if (call is null)
        {
            return;
        }

        var endedAt = dateTimeProvider.UtcNow;

        // The room is about to stop existing, so still-open participant rows must be closed out
        // synchronously here rather than relying on their CallParticipantRemoved webhooks — those
        // events would arrive after EndedAt is already set and no-op against a call that looks ended.
        foreach (var participant in call.Participants.Where(p => p.LeftAt is null))
        {
            participant.LeftAt = endedAt;
            boardCallRepository.UpdateParticipant(participant);
        }

        call.EndedAt = endedAt;
        boardCallRepository.Update(call);
        await unitOfWork.SaveChangesAsync(ct);

        await acsCallService.DeleteRoomAsync(call.AcsRoomId, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            call.BoardId,
            BoardActionNotificationType.CallEnded,
            endedByUserId ?? call.StartedByUserId,
            endedAt,
            new CallEndedPayload(call.Id)), ct);
    }

    public async Task EndCallIfEmptyAsync(Guid boardCallId, CancellationToken ct = default)
    {
        var activeParticipantCount = await boardCallRepository.CountActiveParticipantsAsync(boardCallId, ct);

        if (activeParticipantCount == 0)
        {
            await EndCallAsync(boardCallId, ct: ct);
        }
    }
}
