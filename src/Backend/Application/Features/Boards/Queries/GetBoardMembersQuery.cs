using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardMembersQuery(Guid BoardId) : IRequest<List<BoardMemberDto>>;

public class GetBoardMembersQueryHandler(IBoardRepository boardRepository, IBoardAccessService boardAccessService)
    : IRequestHandler<GetBoardMembersQuery, List<BoardMemberDto>>
{
    public async Task<List<BoardMemberDto>> Handle(GetBoardMembersQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        return await boardRepository.GetBoardMembersAsync(request.BoardId, ct);
    }
}
