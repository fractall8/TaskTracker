using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetWorkspaceBoardsQuery(Guid WorkspaceId) : IRequest<List<BoardPreviewDto>>;

public class GetWorkspaceBoardsQueryHandler(
    IBoardRepository boardRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetWorkspaceBoardsQuery, List<BoardPreviewDto>>
{
    public async Task<List<BoardPreviewDto>> Handle(GetWorkspaceBoardsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceRole = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, cancellationToken);
        var currentUserId = await workspaceAccessService.GetCurrentUserIdAsync(cancellationToken);

        var boards = await boardRepository.GetBoardsByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);

        var boardDtos = new List<BoardPreviewDto>();

        foreach (var board in boards)
        {
            var boardRole = BoardRole.User;

            if (workspaceRole is WorkspaceRole.Owner or WorkspaceRole.Admin)
            {
                boardRole = BoardRole.Admin;
            }
            else
            {
                var specificBoardRole =
                    await boardRepository.GetUserRoleAsync(board.Id, currentUserId, cancellationToken);
                if (specificBoardRole.HasValue)
                {
                    boardRole = specificBoardRole.Value;
                }
            }

            boardDtos.Add(new BoardPreviewDto(
                board.Id,
                board.Name,
                board.Description,
                board.CreatedAt,
                (Contracts.Enums.BoardRoleDto)boardRole));
        }

        return boardDtos;
    }
}
