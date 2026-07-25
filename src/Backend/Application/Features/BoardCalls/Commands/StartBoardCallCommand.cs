using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Commands;

public record StartBoardCallCommand(Guid BoardId) : IRequest<StartOrJoinBoardCallResponse>;

public class StartBoardCallCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository,
    IAcsCallService acsCallService,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartBoardCallCommand, StartOrJoinBoardCallResponse>
{
    public async Task<StartOrJoinBoardCallResponse> Handle(StartBoardCallCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanStartCallAsync(request.BoardId, ct);

        var existingActiveCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, ct);

        if (existingActiveCall is not null)
        {
            throw new ConflictException("This board already has an active call.");
        }

        var roomId = await acsCallService.CreateRoomAsync(ct);

        var call = new BoardCall
        {
            Id = Guid.NewGuid(),
            BoardId = request.BoardId,
            StartedByUserId = boardAccessContext.UserId,
            AcsRoomId = roomId,
            StartedAt = dateTimeProvider.UtcNow
        };

        // Reserve the starter's own participant seat synchronously, in the same transaction as the
        // BoardCall row, so the capacity count is trustworthy immediately — it no longer depends on
        // the async CallParticipantAdded webhook to know the starter occupies a seat.
        var participant = new BoardCallParticipant
        {
            Id = Guid.NewGuid(),
            BoardCallId = call.Id,
            UserId = boardAccessContext.UserId,
            JoinedAt = call.StartedAt
        };

        string acsUserId;

        try
        {
            // Resolve the starter's ACS identity before the single SaveChangesAsync below, so the identity
            // provisioning (if any), the new BoardCall row, and its participant reservation all commit
            // together in one transaction.
            acsUserId = await acsCallService.EnsureUserIdentityAsync(boardAccessContext.UserId, ct);

            await boardCallRepository.AddAsync(call, ct);
            await boardCallRepository.AddParticipantAsync(participant, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            // Nothing was persisted (a genuine double-start race is translated to ConflictException by
            // UnitOfWork itself) — the ACS room is the only thing left dangling.
            await acsCallService.DeleteRoomAsync(roomId, ct);
            throw;
        }

        try
        {
            await acsCallService.AddOrUpdateParticipantAsync(roomId, acsUserId, CallParticipantRole.Presenter, ct);
            var credentials = await acsCallService.IssueTokenAsync(acsUserId, roomId, ct);

            var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(call.Id, ct) ?? call;
            var callDto = BoardCallMappings.ToDto(callWithParticipants);

            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.CallStarted,
                boardAccessContext.UserId,
                call.StartedAt,
                new CallStartedPayload(callDto)), ct);

            return new StartOrJoinBoardCallResponse(callDto, credentials);
        }
        catch
        {
            // The call never became usable — undo the DB rows and the room so a retry starts clean.
            boardCallRepository.DeleteParticipant(participant);
            boardCallRepository.Delete(call);
            await unitOfWork.SaveChangesAsync(ct);
            await acsCallService.DeleteRoomAsync(roomId, ct);
            throw;
        }
    }
}

public class StartBoardCallCommandValidator : AbstractValidator<StartBoardCallCommand>
{
    public StartBoardCallCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
