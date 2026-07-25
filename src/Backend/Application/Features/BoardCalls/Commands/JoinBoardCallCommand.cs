using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Commands;

public record JoinBoardCallCommand(Guid BoardId) : IRequest<StartOrJoinBoardCallResponse>;

public class JoinBoardCallCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository,
    IAcsCallService acsCallService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<JoinBoardCallCommand, StartOrJoinBoardCallResponse>
{
    public async Task<StartOrJoinBoardCallResponse> Handle(JoinBoardCallCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var activeCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, ct)
                         ?? throw new NotFoundException("No active call found for this board.");

        // A caller who already holds an active seat (e.g. reconnecting after a page refresh) is not
        // a new join — skip the capacity check and don't reserve a second seat for them.
        var existingParticipant = await boardCallRepository.GetActiveParticipantAsync(activeCall.Id, boardAccessContext.UserId, ct);

        if (existingParticipant is null)
        {
            var activeParticipantCount = await boardCallRepository.CountActiveParticipantsAsync(activeCall.Id, ct);

            if (activeParticipantCount >= BoardCallConstants.MaxParticipants)
            {
                throw new ConflictException(
                    $"This call is full ({BoardCallConstants.MaxParticipants}/{BoardCallConstants.MaxParticipants} participants).");
            }
        }

        var role = ToCallParticipantRole(boardAccessContext.Role);

        var acsUserId = await acsCallService.EnsureUserIdentityAsync(boardAccessContext.UserId, ct);

        // Reserve the seat synchronously (for a genuinely new join) so the capacity count above is
        // trustworthy for the next caller immediately — it no longer depends on the async
        // CallParticipantAdded webhook to know this participant occupies a seat.
        BoardCallParticipant? reservedParticipant = null;

        if (existingParticipant is null)
        {
            reservedParticipant = new BoardCallParticipant
            {
                Id = Guid.NewGuid(),
                BoardCallId = activeCall.Id,
                UserId = boardAccessContext.UserId,
                JoinedAt = dateTimeProvider.UtcNow
            };

            await boardCallRepository.AddParticipantAsync(reservedParticipant, ct);
        }

        // EnsureUserIdentityAsync only mutates the tracked User entity — this handler owns the commit,
        // which also persists the reservation (if any) in the same transaction.
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            await acsCallService.AddOrUpdateParticipantAsync(activeCall.AcsRoomId, acsUserId, role, ct);
            var credentials = await acsCallService.IssueTokenAsync(acsUserId, activeCall.AcsRoomId, ct);

            var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(activeCall.Id, ct)
                                        ?? activeCall;

            return new StartOrJoinBoardCallResponse(BoardCallMappings.ToDto(callWithParticipants), credentials);
        }
        catch when (reservedParticipant is not null)
        {
            // The join never became usable — release the reserved seat so it isn't stuck occupied.
            boardCallRepository.DeleteParticipant(reservedParticipant);
            await unitOfWork.SaveChangesAsync(ct);
            throw;
        }
    }

    private static CallParticipantRole ToCallParticipantRole(BoardRole role) => role switch
    {
        BoardRole.Admin or BoardRole.ScrumMaster => CallParticipantRole.Presenter,
        _ => CallParticipantRole.Attendee
    };
}

public class JoinBoardCallCommandValidator : AbstractValidator<JoinBoardCallCommand>
{
    public JoinBoardCallCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
