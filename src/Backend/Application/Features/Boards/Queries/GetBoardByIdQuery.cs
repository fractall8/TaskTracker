using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardByIdQuery(Guid BoardId) : IRequest<BoardWithColumnsDto>;

public class GetBoardByIdQueryHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository)
    : IRequestHandler<GetBoardByIdQuery, BoardWithColumnsDto>
{
    public async Task<BoardWithColumnsDto> Handle(GetBoardByIdQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);
        
        var board = await boardRepository.GetBoardWithHierarchyAsync(request.BoardId, ct);
        
        if (board is null)
        {
            throw new KeyNotFoundException($"Board {request.BoardId} does not exist");
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
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");
    }
}