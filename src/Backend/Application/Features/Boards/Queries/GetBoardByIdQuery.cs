using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardByIdQuery(Guid Id) : IRequest<BoardWithColumnsDto>;

public class GetBoardByIdQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository)
    : IRequestHandler<GetBoardByIdQuery, BoardWithColumnsDto>
{
    public async Task<BoardWithColumnsDto> Handle(GetBoardByIdQuery request, CancellationToken ct)
    {
        var board = await boardRepository.GetBoardWithHierarchyAsync(request.Id, ct);
        if (board is null)
        {
            throw new KeyNotFoundException($"Board {request.Id} does not exist");
        }

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => (Guid?)u.Id, ct);
        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userRole = await boardRepository.GetUserRoleAsync(request.Id, currentUserId.Value, ct);
        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this board.");
        }

        var columnDtos = board.Columns
            .OrderBy(c => c.Position)
            .Select(c => new ColumnDto(c.Id, c.Name, c.Position))
            .ToList();

        return new BoardWithColumnsDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            Columns: columnDtos
            );
    }
}

public class GetBoardByIdQueryValidator : AbstractValidator<GetBoardByIdQuery>
{
    public GetBoardByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Board ID is required.");
    }
}