using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Contracts.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Boards.Queries;

public record GetMyArchivedWorkspaceBoardsQuery(
    Guid WorkspaceId,
    int PageNumber,
    int PageSize,
    string? SearchTerm) : IRequest<PagedList<BoardPreviewDto>>;

public class GetMyArchivedWorkspaceBoardsQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    IBoardRepository boardRepository)
    : IRequestHandler<GetMyArchivedWorkspaceBoardsQuery, PagedList<BoardPreviewDto>>
{
    public async Task<PagedList<BoardPreviewDto>> Handle(
        GetMyArchivedWorkspaceBoardsQuery request,
        CancellationToken ct)
    {
        var userInfo = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var totalCount =
            await boardRepository.CountArchivedMemberWorkspaceBoardsAsync(request.WorkspaceId, userInfo.UserId,
                request.SearchTerm, ct);

        var boards = await boardRepository.GetMyArchivedWorkspaceBoardsAsync(
            request.WorkspaceId,
            userInfo.UserId,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            ct);

        var boardDtos = boards.Select(board =>
        {
            var userMemberRecord = board.Members.First(m => m.WorkspaceMember!.UserId == userInfo.UserId);

            return new BoardPreviewDto(
                board.Id,
                board.Name,
                board.Description,
                board.CreatedAt,
                (BoardRoleDto)userMemberRecord.Role,
                board.IsArchived);
        }).ToList();

        return new PagedList<BoardPreviewDto>
        {
            Metadata = new PaginationMetadata
                { CurrentPage = request.PageNumber, PageSize = request.PageSize, TotalCount = totalCount },
            Items = boardDtos
        };
    }
}

public class GetMyArchivedWorkspaceBoardsQueryValidator : AbstractValidator<GetMyArchivedWorkspaceBoardsQuery>
{
    public GetMyArchivedWorkspaceBoardsQueryValidator(IOptions<PaginationOptions> options)
    {
        var paginationOptions = options.Value;

        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(paginationOptions.MaxPageSize);
    }
}
