using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardArchiveDownloadQuery(Guid BoardId) : IRequest<BoardArchiveDownloadDto>;

public class GetBoardArchiveDownloadQueryHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IBoardExportService boardExportService
    )
    : IRequestHandler<GetBoardArchiveDownloadQuery, BoardArchiveDownloadDto>
{
    public async Task<BoardArchiveDownloadDto> Handle(GetBoardArchiveDownloadQuery request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanExportBoardAsync(request.BoardId, cancellationToken);

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken);
        if (board == null)
        {
            throw new KeyNotFoundException("Board not found.");
        }

        var exportInfo = await boardExportService.GetBoardExportInfoAsync(request.BoardId, cancellationToken);

        if (exportInfo == null)
        {
            throw new KeyNotFoundException("Board export info not found.");
        }

        if (exportInfo.ExportStatus != BoardExportStatusDto.Completed)
        {
            throw new InvalidOperationException("The board archive export is not completed yet.");
        }

        // TODO: check file name

        // TODO: get download url (SAS-token)

        return new BoardArchiveDownloadDto("downloadUrl", "fileName");
    }
}
