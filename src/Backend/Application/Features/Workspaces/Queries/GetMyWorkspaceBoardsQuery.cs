using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetMyWorkspaceBoardsQuery(Guid WorkspaceId, int PageNumber, int PageSize, string? SearchTerm)
    : IRequest<PagedList<BoardPreviewDto>>;

public class GetMyWorkspaceBoardsQueryHandler(
    IBoardRepository boardRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetMyWorkspaceBoardsQuery, PagedList<BoardPreviewDto>>
{
    public async Task<PagedList<BoardPreviewDto>> Handle(GetMyWorkspaceBoardsQuery request, CancellationToken ct)
    {
        var userInfo = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var totalCount =
            await boardRepository.CountMemberWorkspaceBoardsAsync(request.WorkspaceId, userInfo.UserId,
                request.SearchTerm, ct);

        var boards = await boardRepository.GetMemberWorkspaceBoardsPaginatedAsync(
            request.WorkspaceId, userInfo.UserId, request.PageNumber, request.PageSize, request.SearchTerm, ct);

        var boardDtos = boards.Select(board =>
        {
            var userMemberRecord = board.Members.First(m => m.WorkspaceMember!.UserId == userInfo.UserId);

            return new BoardPreviewDto(
                board.Id,
                board.Name,
                board.Description,
                board.CreatedAt,
                (BoardRoleDto)userMemberRecord.Role);
        }).ToList();

        return new PagedList<BoardPreviewDto>
        {
            Metadata = new PaginationMetadata
                { CurrentPage = request.PageNumber, PageSize = request.PageSize, TotalCount = totalCount },
            Items = boardDtos
        };
    }
}
