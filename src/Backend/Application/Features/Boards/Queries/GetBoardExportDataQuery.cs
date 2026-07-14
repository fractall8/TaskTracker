using Application.Interfaces.Repositories;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardExportDataQuery(Guid BoardId, BoardExportOptionsDto ExportOptions) : IRequest<BoardExportDataDto?>;

public class GetBoardExportDataQueryHandler(IBoardRepository boardRepository)
    : IRequestHandler<GetBoardExportDataQuery, BoardExportDataDto?>
{
    public async Task<BoardExportDataDto?> Handle(
        GetBoardExportDataQuery request,
        CancellationToken ct)
    {
        var data = await boardRepository.GetBoardExportDataAsync(
            request.BoardId,
            request.ExportOptions,
            ct);

        return data;
    }
}

public class GetBoardExportDataQueryValidator : AbstractValidator<GetBoardExportDataQuery>
{
    public GetBoardExportDataQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ExportOptions).NotNull();
    }
}
