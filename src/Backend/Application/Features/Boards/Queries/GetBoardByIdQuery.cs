using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardByIdQuery(Guid BoardId, string? SearchTerm = null) : IRequest<BoardWithColumnsDto>;

public class GetBoardByIdQueryHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository)
    : IRequestHandler<GetBoardByIdQuery, BoardWithColumnsDto>
{
    public async Task<BoardWithColumnsDto> Handle(GetBoardByIdQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);
        
        var board = await boardRepository.GetBoardWithHierarchyAsync(request.BoardId, request.SearchTerm, ct);
        
        if (board is null)
        {
            throw new KeyNotFoundException($"Board {request.BoardId} does not exist");
        }

        var columnDtos = board.Columns
            .OrderBy(c => c.Position)
            .Select(c => new ColumnDto(
                Id: c.Id, 
                Name: c.Name, 
                Position: c.Position,
                Tasks: c.Tasks.OrderBy(t => t.Position).Select(t => new TaskDto(
                    Id: t.Id,
                    Title: t.Title,
                    Description: t.Description,
                    Position: t.Position,
                    DueDate: t.DueDate,
                    ColumnId: t.ColumnId,
                    AssigneeId: t.AssigneeId,
                    ReporterId: t.ReporterId,
                    Attachments: new List<AttachmentDto>()
                )).ToList()
            ))
            .ToList();

        return new BoardWithColumnsDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            Columns: columnDtos
        );
    }
}