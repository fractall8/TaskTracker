using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Commands;

public record LeaveBoardCallCommand(Guid BoardId) : IRequest;

public class LeaveBoardCallCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository,
    IUserRepository userRepository,
    IAcsCallService acsCallService,
    IBoardCallLifecycleService boardCallLifecycleService,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LeaveBoardCallCommand>
{
    public async Task Handle(LeaveBoardCallCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var activeCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, ct);

        if (activeCall is null)
        {
            return;
        }

        var user = await userRepository.GetByIdAsync(boardAccessContext.UserId, ct);

        if (string.IsNullOrWhiteSpace(user?.AcsCommunicationUserId))
        {
            // Never provisioned an ACS identity, so never actually joined a call — nothing to revoke.
            return;
        }

        await acsCallService.RemoveParticipantAsync(activeCall.AcsRoomId, user.AcsCommunicationUserId, ct);

        // Mark the seat vacated synchronously, mirroring Start/Join's synchronous reservation (the same
        // explicit decision that moved capacity-affecting participant state off the async Event Grid
        // webhook) — otherwise the seat stays counted as occupied, blocking a new join at capacity, until
        // the webhook eventually confirms it. RecordCallParticipantLeftCommand's own active-only lookup
        // already tolerates a redundant/duplicate delivery for this same event as a safe no-op.
        var participant = await boardCallRepository.GetActiveParticipantAsync(activeCall.Id, boardAccessContext.UserId, ct);

        if (participant is null)
        {
            return;
        }

        var leftAt = dateTimeProvider.UtcNow;
        participant.LeftAt = leftAt;
        boardCallRepository.UpdateParticipant(participant);
        await unitOfWork.SaveChangesAsync(ct);

        var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(activeCall.Id, ct);

        if (callWithParticipants is not null)
        {
            // Null means the call ended concurrently with this request (e.g. a ScrumMaster ended it for
            // everyone at the same moment) — nothing meaningful left to notify participants about for a
            // call that's already over.
            var participants = BoardCallMappings.ToParticipantDtos(callWithParticipants);

            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                activeCall.BoardId,
                BoardActionNotificationType.CallParticipantsChanged,
                boardAccessContext.UserId,
                leftAt,
                new CallParticipantsChangedPayload(activeCall.Id, participants)), ct);
        }

        await boardCallLifecycleService.EndCallIfEmptyAsync(activeCall.Id, ct);
    }
}

public class LeaveBoardCallCommandValidator : AbstractValidator<LeaveBoardCallCommand>
{
    public LeaveBoardCallCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
