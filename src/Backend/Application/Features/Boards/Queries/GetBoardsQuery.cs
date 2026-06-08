using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardsQuery : IRequest<IEnumerable<BoardPreviewDto>>;

public class GetBoardsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository)
    : IRequestHandler<GetBoardsQuery, IEnumerable<BoardPreviewDto>>
{
    public async Task<IEnumerable<BoardPreviewDto>> Handle(GetBoardsQuery request, CancellationToken ct)
    {
        Console.WriteLine($"[DEBUG] Accessor AzureAdObjectId: {currentUserAccessor.AzureAdObjectId}");
        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(
                                currentUserAccessor.AzureAdObjectId, 
                                u => (Guid?)u.Id, 
                                ct) 
                            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var boards = await boardRepository.GetUserBoardsAsync(currentUserId, ct);

        return boards.Select(board => new BoardPreviewDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt,
            Role: (Contracts.Enums.BoardRoleDto)board.Members.First().Role 
        ));
    }
}