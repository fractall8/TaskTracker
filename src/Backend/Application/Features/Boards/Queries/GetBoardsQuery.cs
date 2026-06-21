using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Boards.Queries;

public record GetBoardsQuery(int PageNumber, int PageSize, string? SearchTerm) : IRequest<PagedList<BoardPreviewDto>>;

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

        var totalCount = await boardRepository.CountUserBoardsAsync(currentUserId, request.SearchTerm, ct);

        var boards = await boardRepository.GetUserBoardsPaginatedAsync(
            currentUserId,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            ct);

        var boardDtos = boards.Select(board => new BoardPreviewDto(
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

public class GetBoardsQueryValidator : AbstractValidator<GetBoardsQuery>
{
    public GetBoardsQueryValidator(IOptions<PaginationOptions> options)
    {
        var paginationOptions = options.Value;

        RuleFor(v => v.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, paginationOptions.MaxPageSize).WithMessage($"Page size must be between 1 and {paginationOptions.MaxPageSize}.");

        RuleFor(v => v.SearchTerm)
            .MaximumLength(paginationOptions.MaxSearchTermLength).WithMessage("Search term must not exceed 100 characters.");
    }
}