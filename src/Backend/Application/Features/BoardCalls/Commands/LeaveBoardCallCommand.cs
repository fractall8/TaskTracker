using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Commands;

public record LeaveBoardCallCommand(Guid BoardId) : IRequest;

public class LeaveBoardCallCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository,
    IUserRepository userRepository,
    IAcsCallService acsCallService)
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
    }
}

public class LeaveBoardCallCommandValidator : AbstractValidator<LeaveBoardCallCommand>
{
    public LeaveBoardCallCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
