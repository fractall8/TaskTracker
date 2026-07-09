using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Boards.Commands;

public record ArchiveAndExportBoardCommand(Guid BoardId, BoardExportOptionsDto ExportOptions)
    : IRequest<BoardArchivedDto>;

public class ArchiveAndExportBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IBoardExportService boardExportService,
    IBoardExportStatusNotifier exportStatusNotifier,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<ArchiveAndExportBoardCommandHandler> logger)
    : IRequestHandler<ArchiveAndExportBoardCommand, BoardArchivedDto>
{
    public async Task<BoardArchivedDto> Handle(ArchiveAndExportBoardCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanExportBoardAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);
        if (board == null)
        {
            throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");
        }

        var archivedAt = dateTimeProvider.UtcNow;
        board.IsArchived = true;
        board.ArchivedAt = archivedAt;

        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            await boardExportService.SetExportAsync(
                board.Id,
                BoardExportStatusDto.Requested,
                request.ExportOptions,
                ct);

            await exportStatusNotifier.NotifyExportStatusChangedAsync(
                new BoardExportStatusChangedNotification(board.Id, BoardExportStatusDto.Requested),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Board {BoardId} was archived, but failed to start export process in CosmosDB.", board.Id);
            throw new InvalidOperationException($"Board archived, but failed to initialize export for board {request.BoardId}.", ex);
        }

        return new BoardArchivedDto(archivedAt, BoardExportStatusDto.Requested);
    }
}

public class ArchiveAndExportBoardCommandValidator : AbstractValidator<ArchiveAndExportBoardCommand>
{
    public ArchiveAndExportBoardCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.ExportOptions)
            .NotNull();
    }
}
