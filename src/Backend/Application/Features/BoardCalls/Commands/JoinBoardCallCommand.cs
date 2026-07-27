using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.BoardCalls.Commands;

public record JoinBoardCallCommand(Guid BoardId) : IRequest<StartOrJoinBoardCallResponse>;

public class JoinBoardCallCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository,
    IBoardMemberRepository boardMemberRepository,
    IAcsCallService acsCallService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    ILogger<JoinBoardCallCommandHandler> logger)
    : IRequestHandler<JoinBoardCallCommand, StartOrJoinBoardCallResponse>
{
    public async Task<StartOrJoinBoardCallResponse> Handle(JoinBoardCallCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var activeCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, ct)
                         ?? throw new NotFoundException("No active call found for this board.");

        var role = ToCallParticipantRole(boardAccessContext.Role);

        string acsUserId = null!;
        BoardCallParticipant? reservedParticipant = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // Serializes concurrent joins against the same call so the capacity check below can never
            // read a stale count by the time this join's seat reservation commits — closes the residual
            // TOCTOU race where two concurrent new joins both read the count before either commits.
            await unitOfWork.AcquireDistributedLockAsync($"boardcall:{activeCall.Id}:participants", token);

            // A caller who already holds an active seat (e.g. reconnecting after a page refresh) is not
            // a new join — skip the capacity check and don't reserve a second seat for them.
            var existingParticipant = await boardCallRepository.GetActiveParticipantAsync(activeCall.Id, boardAccessContext.UserId, token);

            if (existingParticipant is null)
            {
                var activeParticipantCount = await boardCallRepository.CountActiveParticipantsAsync(activeCall.Id, token);

                // The call's capacity is the board's own membership — a call can never hold more people
                // than the board actually has members, and shrinks/grows with board membership rather
                // than an arbitrary fixed number.
                var maxParticipants = await boardMemberRepository.CountByBoardIdAsync(request.BoardId, token);

                if (activeParticipantCount >= maxParticipants)
                {
                    throw new ConflictException(
                        $"This call is full ({maxParticipants}/{maxParticipants} participants).");
                }

                // Reserve the seat synchronously so the capacity count above is trustworthy for the next
                // caller immediately — it no longer depends on the async CallParticipantAdded webhook to
                // know this participant occupies a seat.
                reservedParticipant = new BoardCallParticipant
                {
                    Id = Guid.NewGuid(),
                    BoardCallId = activeCall.Id,
                    UserId = boardAccessContext.UserId,
                    JoinedAt = dateTimeProvider.UtcNow
                };

                await boardCallRepository.AddParticipantAsync(reservedParticipant, token);
            }

            // Also serializes against a concurrent identity provisioning for the same user (e.g. joining
            // two different boards' calls at once) — without this, two concurrent callers can both see no
            // AcsCommunicationUserId and both create a new ACS identity, silently orphaning one of them
            // via a last-writer-wins update. Re-reads the user row fresh after acquiring the lock, so a
            // caller that loses the race sees the winner's already-committed identity instead of racing it.
            await unitOfWork.AcquireDistributedLockAsync($"user:{boardAccessContext.UserId}:acs-identity", token);
            acsUserId = await acsCallService.EnsureUserIdentityAsync(boardAccessContext.UserId, token);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        try
        {
            await acsCallService.AddOrUpdateParticipantAsync(activeCall.AcsRoomId, acsUserId, role, ct);
            var credentials = await acsCallService.IssueTokenAsync(acsUserId, activeCall.AcsRoomId, ct);

            var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(activeCall.Id, ct)
                                        ?? activeCall;
            var maxParticipants = await boardMemberRepository.CountByBoardIdAsync(request.BoardId, ct);

            return new StartOrJoinBoardCallResponse(BoardCallMappings.ToDto(callWithParticipants, maxParticipants), credentials);
        }
        catch when (reservedParticipant is not null)
        {
            // The join never became usable — release the reserved seat so it isn't stuck occupied.
            // Cleanup failure is only logged, never rethrown, so it can't mask the original exception
            // the caller actually needs to see.
            try
            {
                boardCallRepository.DeleteParticipant(reservedParticipant);
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception cleanupEx)
            {
                logger.LogError(cleanupEx, "Failed to release reserved seat {ParticipantId} after a failed join", reservedParticipant.Id);
            }

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
