using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Columns.Commands;

public record DeleteColumnCommand(Guid BoardId, Guid ColumnId) : IRequest;

public class DeleteColumnCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<DeleteColumnCommandHandler> logger)
    : IRequestHandler<DeleteColumnCommand>
{
    public async Task Handle(DeleteColumnCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageColumnsAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board is null)
        {
            throw new KeyNotFoundException($"Board {request.BoardId} does not exist");
        }

        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);

        if (column is null || column.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException($"Column {request.ColumnId} does not exist on this board");
        }

        var positionToShift = column.Position;
        var fileUrlsToDelete = await attachmentRepository.GetUrlsByColumnIdAsync(request.ColumnId, ct);

        await unitOfWork.ExecuteInTransactionAsync(async (token) =>
        {
            await columnRepository.SoftDeleteCascadeAsync(request.ColumnId, token);
            await columnRepository.DecrementPositionsAsync(request.BoardId, positionToShift, token);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        var updatedRemainingColumns = await columnRepository.GetListByBoardIdAsync(request.BoardId, ct);

        foreach (var fileUrl in fileUrlsToDelete)
        {
            try
            {
                await fileService.DeleteFileAsync(fileUrl, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphaned blob for Column {ColumnId}: {FileUrl}", request.ColumnId, fileUrl);
            }
        }

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.ColumnDeleted,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new ColumnDeletedPayload(
                request.ColumnId,
                BoardActionPositionMappings.ToColumnPositions(updatedRemainingColumns))), ct);
    }
}

public class DeleteColumnCommandValidator : AbstractValidator<DeleteColumnCommand>
{
    public DeleteColumnCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Column ID is required.");
    }
}
