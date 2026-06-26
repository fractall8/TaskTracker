using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Contracts.Enums;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Workspaces.Queries;

public record GetWorkspaceBoardsQuery(Guid WorkspaceId, int PageNumber, int PageSize, string? SearchTerm)
    : IRequest<PagedList<BoardPreviewDto>>;

public class GetWorkspaceBoardsQueryHandler(
    IBoardRepository boardRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetWorkspaceBoardsQuery, PagedList<BoardPreviewDto>>
{
    public async Task<PagedList<BoardPreviewDto>> Handle(GetWorkspaceBoardsQuery request, CancellationToken ct)
    {
        var userInfo = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var totalCount =
            await boardRepository.CountBoardsByWorkspaceIdAsync(request.WorkspaceId, userInfo.Id, request.SearchTerm,
                ct);

        var boards = await boardRepository.GetBoardsByWorkspaceIdPaginatedAsync(
            request.WorkspaceId,
            userInfo.Id,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            ct);

        var boardDtos = new List<BoardPreviewDto>();

        foreach (var board in boards)
        {
            var boardRole = BoardRole.User;

            if (userInfo.Role is WorkspaceRole.Owner or WorkspaceRole.Admin)
            {
                boardRole = BoardRole.Admin;
            }
            else
            {
                var specificBoardRole = await boardRepository.GetUserRoleAsync(board.Id, userInfo.Id, ct);
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
                (BoardRoleDto)boardRole));
        }

        return new PagedList<BoardPreviewDto>
        {
            Metadata = new PaginationMetadata
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            },
            Items = boardDtos
        };
    }
}

public class GetWorkspaceBoardsQueryValidator : AbstractValidator<GetWorkspaceBoardsQuery>
{
    public GetWorkspaceBoardsQueryValidator(IOptions<PaginationOptions> options)
    {
        var paginationOptions = options.Value;

        RuleFor(v => v.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, paginationOptions.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {paginationOptions.MaxPageSize}.");

        RuleFor(v => v.SearchTerm)
            .MaximumLength(paginationOptions.MaxSearchTermLength)
            .WithMessage("Search term must not exceed 100 characters.");
    }
}
