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

public record RecordCallParticipantLeftCommand(string AcsRoomId, string AcsUserRawId, DateTimeOffset OccurredAt) : IRequest;

public class RecordCallParticipantLeftCommandHandler(
    IBoardCallRepository boardCallRepository,
    IUserRepository userRepository,
    IBoardActionNotifier boardActionNotifier,
    IBoardCallLifecycleService boardCallLifecycleService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RecordCallParticipantLeftCommand>
{
    public async Task Handle(RecordCallParticipantLeftCommand request, CancellationToken ct)
    {
        var call = await boardCallRepository.GetActiveCallByAcsRoomIdAsync(request.AcsRoomId, ct);

        if (call is null)
        {
            // The call is already ended (or was never one of ours) — nothing to update.
            return;
        }

        var user = await userRepository.GetByAcsCommunicationUserIdAsync(request.AcsUserRawId, ct);

        if (user is null)
        {
            return;
        }

        var participant = await boardCallRepository.GetActiveParticipantAsync(call.Id, user.Id, ct);

        if (participant is null)
        {
            // Already marked left (or never recorded as joined) — Event Grid is at-least-once, safe no-op.
            // Still worth checking: this delivery could be the one that finally empties the call.
            await boardCallLifecycleService.EndCallIfEmptyAsync(call.Id, ct);
            return;
        }

        participant.LeftAt = request.OccurredAt;
        boardCallRepository.UpdateParticipant(participant);
        await unitOfWork.SaveChangesAsync(ct);

        var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(call.Id, ct) ?? call;
        var participants = BoardCallMappings.ToDto(callWithParticipants).Participants;

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            call.BoardId,
            BoardActionNotificationType.CallParticipantsChanged,
            user.Id,
            request.OccurredAt,
            new CallParticipantsChangedPayload(call.Id, participants)), ct);

        await boardCallLifecycleService.EndCallIfEmptyAsync(call.Id, ct);
    }
}

public class RecordCallParticipantLeftCommandValidator : AbstractValidator<RecordCallParticipantLeftCommand>
{
    public RecordCallParticipantLeftCommandValidator()
    {
        RuleFor(x => x.AcsRoomId).NotEmpty();
        RuleFor(x => x.AcsUserRawId).NotEmpty();
    }
}
