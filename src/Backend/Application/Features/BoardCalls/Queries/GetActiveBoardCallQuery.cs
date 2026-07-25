using Application.Common.Mappings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.BoardCalls.Queries;

public record GetActiveBoardCallQuery(Guid BoardId) : IRequest<BoardCallDto?>;

public class GetActiveBoardCallQueryHandler(
    IBoardAccessService boardAccessService,
    IBoardCallRepository boardCallRepository)
    : IRequestHandler<GetActiveBoardCallQuery, BoardCallDto?>
{
    public async Task<BoardCallDto?> Handle(GetActiveBoardCallQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var activeCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, ct);

        if (activeCall is null)
        {
            return null;
        }

        var callWithParticipants = await boardCallRepository.GetActiveCallWithParticipantsAsync(activeCall.Id, ct)
                                    ?? activeCall;

        return BoardCallMappings.ToDto(callWithParticipants);
    }
}

public class GetActiveBoardCallQueryValidator : AbstractValidator<GetActiveBoardCallQuery>
{
    public GetActiveBoardCallQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
