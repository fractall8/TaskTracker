using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Commands;

public record RecordCallParticipantJoinedCommand(string AcsRoomId, string AcsUserRawId, DateTimeOffset OccurredAt) : IRequest;

public class RecordCallParticipantJoinedCommandHandler(
    IBoardCallRepository boardCallRepository,
    IUserRepository userRepository,
    IBoardActionNotifier boardActionNotifier,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RecordCallParticipantJoinedCommand>
{
    public async Task Handle(RecordCallParticipantJoinedCommand request, CancellationToken ct)
    {
        var call = await boardCallRepository.GetActiveCallByAcsRoomIdAsync(request.AcsRoomId, ct);

        if (call is null)
        {
            return;
        }

        var user = await userRepository.GetByAcsCommunicationUserIdAsync(request.AcsUserRawId, ct);

        if (user is null)
        {
            return;
        }

        // Event Grid is at-least-once and can redeliver or reorder events, so compare against the most
        // recent participant row for this user+call (active or not) rather than only the active one —
        // otherwise a stale join arriving after a newer leave (or a duplicate of the current active
        // join) would incorrectly reopen/duplicate the participant as active.
        var latestParticipant = await boardCallRepository.GetLatestParticipantAsync(call.Id, user.Id, ct);

        if (latestParticipant is not null && (latestParticipant.LeftAt is null || latestParticipant.LeftAt >= request.OccurredAt))
        {
            return;
        }

        await boardCallRepository.AddParticipantAsync(new BoardCallParticipant
        {
            Id = Guid.NewGuid(),
            BoardCallId = call.Id,
            UserId = user.Id,
            JoinedAt = request.OccurredAt
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);

        var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(call.Id, ct);

        if (callWithParticipants is null)
        {
            // The call ended concurrently with this webhook's processing — nothing meaningful left to
            // notify participants about for a call that's already over.
            return;
        }

        var participants = BoardCallMappings.ToParticipantDtos(callWithParticipants);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            call.BoardId,
            BoardActionNotificationType.CallParticipantsChanged,
            user.Id,
            request.OccurredAt,
            new CallParticipantsChangedPayload(call.Id, participants)), ct);
    }
}

public class RecordCallParticipantJoinedCommandValidator : AbstractValidator<RecordCallParticipantJoinedCommand>
{
    public RecordCallParticipantJoinedCommandValidator()
    {
        RuleFor(x => x.AcsRoomId).NotEmpty();
        RuleFor(x => x.AcsUserRawId).NotEmpty();
    }
}
