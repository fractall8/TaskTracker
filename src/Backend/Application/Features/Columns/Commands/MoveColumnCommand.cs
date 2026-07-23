using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record MoveColumnCommand(Guid BoardId, Guid ColumnId, int NewPosition) : IRequest;

public class MoveColumnCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MoveColumnCommand>
{
    public async Task Handle(MoveColumnCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageColumnsAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board is null)
        {
            throw new NotFoundException($"Board {request.BoardId} does not exist");
        }

        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);

        if (column is null || column.BoardId != request.BoardId)
        {
            throw new NotFoundException($"Column {request.ColumnId} does not exist on this board");
        }

        var maxPosition = await columnRepository.GetMaxPositionAsync(request.BoardId, ct);
        var safeNewPosition = Math.Min(request.NewPosition, maxPosition);

        if (column.Position == safeNewPosition)
        {
            return;
        }

        var oldPosition = column.Position;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await columnRepository.UpdatePositionsOnMoveAsync(request.BoardId, oldPosition, safeNewPosition, token);

            column.Position = safeNewPosition;
            columnRepository.Update(column);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        var reorderedColumns = await columnRepository.GetListByBoardIdAsync(request.BoardId, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.ColumnsReordered,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new ColumnsReorderedPayload(BoardActionPositionMappings.ToColumnPositions(reorderedColumns))), ct);
    }
}

public class MoveColumnCommandValidator : AbstractValidator<MoveColumnCommand>
{
    public MoveColumnCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Column ID is required.");

        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(0).WithMessage("New position must be greater than or equal to 0.");
    }
}
