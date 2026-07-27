using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Commands;

public record EndBoardCallCommand(Guid BoardId) : IRequest;

public class EndBoardCallCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository,
    IBoardCallLifecycleService boardCallLifecycleService)
    : IRequestHandler<EndBoardCallCommand>
{
    public async Task Handle(EndBoardCallCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanEndCallAsync(request.BoardId, ct);

        var activeCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, ct)
                         ?? throw new NotFoundException("No active call found for this board.");

        // Deleting the ACS room and closing out participant rows both happen inside EndCallAsync now,
        // in the same transaction as EndedAt — see BoardCallLifecycleService.
        await boardCallLifecycleService.EndCallAsync(activeCall.Id, boardAccessContext.UserId, ct);
    }
}

public class EndBoardCallCommandValidator : AbstractValidator<EndBoardCallCommand>
{
    public EndBoardCallCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
