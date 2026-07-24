using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.Constants;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Boards.Queries;

public record GetBoardArchiveDownloadQuery(Guid BoardId) : IRequest<BoardArchiveDownloadDto>;

public class GetBoardArchiveDownloadQueryHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IBoardExportService boardExportService,
    IWorkspaceEntitlementService entitlementService,
    IFileService fileService
    )
    : IRequestHandler<GetBoardArchiveDownloadQuery, BoardArchiveDownloadDto>
{
    public async Task<BoardArchiveDownloadDto> Handle(GetBoardArchiveDownloadQuery request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanExportBoardAsync(request.BoardId, cancellationToken);

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken);
        if (board == null)
        {
            throw new NotFoundException("Board not found.");
        }

        var allowedExport =
            await entitlementService.HasFeatureAsync(board.WorkspaceId, FeatureConstants.BoardArchiveDownload, cancellationToken);
        if (!allowedExport)
        {
            throw new SubscriptionFeatureRequiredException(FeatureConstants.BoardArchiveDownload);
        }

        var exportInfo = await boardExportService.GetBoardExportInfoAsync(request.BoardId, cancellationToken);

        if (exportInfo == null)
        {
            throw new NotFoundException("Board export info not found.");
        }

        if (exportInfo.ExportStatus != BoardExportStatusDto.Completed)
        {
            throw new BusinessRuleValidationException("The board archive export is not completed yet.");
        }

        var containerName = BlobContainerNames.ArchiveBoard;

        var prefix = $"{request.BoardId:D}/";

        var (exists, downloadUrl, fileName) = await fileService.GetDownloadUrlByPrefixAsync(
            containerName,
            prefix,
            TimeSpan.FromMinutes(5),
            cancellationToken);

        if (!exists || downloadUrl == null || fileName == null)
        {
            throw new NotFoundException("The export file is missing from storage.");
        }

        return new BoardArchiveDownloadDto(downloadUrl, fileName);
    }
}
