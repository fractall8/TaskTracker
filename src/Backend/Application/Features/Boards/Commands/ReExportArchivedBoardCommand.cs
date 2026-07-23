using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.Constants;
using Contracts.DTOs;
using Contracts.Notifications.BoardExport;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Boards.Commands;

public record ReExportArchivedBoardCommand(Guid BoardId, BoardExportOptionsDto ReExportOptions)
    : IRequest;

public class ReExportArchivedBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IBoardExportService boardExportService,
    IBoardExportStatusNotifier exportStatusNotifier,
    IWorkspaceEntitlementService entitlementService,
    ILogger<ReExportArchivedBoardCommandHandler> logger)
    : IRequestHandler<ReExportArchivedBoardCommand>
{
    public async Task Handle(ReExportArchivedBoardCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanExportBoardAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);
        if (board == null)
        {
            throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");
        }

        var allowedArchive =
            await entitlementService.HasFeatureAsync(board.WorkspaceId, FeatureConstants.BoardReExport, ct);
        var allowedExport =
            await entitlementService.HasFeatureAsync(board.WorkspaceId, FeatureConstants.BoardArchiveDownload, ct);
        var hasFeatures = allowedArchive && allowedExport;
        if (!hasFeatures)
        {
            throw new UnauthorizedAccessException("This workspace didn't have access to reexport boards feature.");
        }

        if (!board.IsArchived)
        {
            throw new InvalidOperationException(
                $"Board with ID {request.BoardId} is not archived. Only archived boards can be re-exported.");
        }

        var exportInfo = await boardExportService.GetBoardExportInfoAsync(request.BoardId, ct);
        if (exportInfo != null &&
            exportInfo.ReExportStatus is BoardExportStatusDto.Requested
                or BoardExportStatusDto.Pending
                or BoardExportStatusDto.Processing)
        {
            throw new InvalidOperationException("A re-export process is already in progress for this board.");
        }

        try
        {
            await boardExportService.SetReExportAsync(
                board.Id,
                BoardExportStatusDto.Requested,
                request.ReExportOptions,
                ct);

            await exportStatusNotifier.NotifyReExportStatusChangedAsync(
                new BoardExportStatusChangedNotification(board.Id, BoardExportStatusDto.Requested,
                    request.ReExportOptions),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start re-export process in CosmosDB for board {BoardId}.", board.Id);
            throw new InvalidOperationException($"Failed to initialize re-export for board {request.BoardId}.", ex);
        }
    }
}

public class ReExportArchivedBoardCommandValidator : AbstractValidator<ReExportArchivedBoardCommand>
{
    public ReExportArchivedBoardCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.ReExportOptions)
            .NotNull();
    }
}
