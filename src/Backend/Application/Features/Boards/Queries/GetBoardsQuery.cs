using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardsQuery(int PageNumber, int PageSize) : IRequest<PagedList<BoardPreviewDto>>;

public class GetBoardsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository)
    : IRequestHandler<GetBoardsQuery, PagedList<BoardPreviewDto>>
{
    public async Task<PagedList<BoardPreviewDto>> Handle(GetBoardsQuery request, CancellationToken ct)
    {
        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(
                                currentUserAccessor.AzureAdObjectId, 
                                u => (Guid?)u.Id, 
                                ct) 
                            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var totalCount = await boardRepository.CountUserBoardsAsync(currentUserId, ct);
        
        var boards = await boardRepository.GetUserBoardsPaginatedAsync(
            currentUserId, 
            request.PageNumber, 
            request.PageSize, 
            ct);
        
        var boardDtos=  boards.Select(board => new BoardPreviewDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt,
            Role: (Contracts.Enums.BoardRoleDto)board.Members.First().Role 
        )).ToList();

        return new PagedList<BoardPreviewDto>
        {
            Metadata = new PaginationMetadata
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            },
            Items = boardDtos,
        };
    }
}