using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetAllWorkspaceBoardsQuery(Guid WorkspaceId, int PageNumber, int PageSize, string? SearchTerm)
    : IRequest<PagedList<BoardPreviewDto>>;

public class GetAllWorkspaceBoardsQueryHandler(
    IBoardRepository boardRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetAllWorkspaceBoardsQuery, PagedList<BoardPreviewDto>>
{
    public async Task<PagedList<BoardPreviewDto>> Handle(GetAllWorkspaceBoardsQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanManageBoardMembersAsync(request.WorkspaceId, ct);

        var totalCount = await boardRepository.CountAllWorkspaceBoardsAsync(request.WorkspaceId, request.SearchTerm, ct);

        var boards = await boardRepository.GetAllWorkspaceBoardsPaginatedAsync(
            request.WorkspaceId, request.PageNumber, request.PageSize, request.SearchTerm, ct);

        var boardDtos = boards.Select(board => new BoardPreviewDto(
            board.Id,
            board.Name,
            board.Description,
            board.CreatedAt,
            BoardRoleDto.Admin
        )).ToList();

        return new PagedList<BoardPreviewDto>
        {
            Metadata = new PaginationMetadata { CurrentPage = request.PageNumber, PageSize = request.PageSize, TotalCount = totalCount },
            Items = boardDtos
        };
    }
}
