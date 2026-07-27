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
    IBoardCallRepository boardCallRepository,
    IBoardMemberRepository boardMemberRepository)
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
        var maxParticipants = await boardMemberRepository.CountByBoardIdAsync(request.BoardId, ct);

        return BoardCallMappings.ToDto(callWithParticipants, maxParticipants);
    }
}

public class GetActiveBoardCallQueryValidator : AbstractValidator<GetActiveBoardCallQuery>
{
    public GetActiveBoardCallQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
