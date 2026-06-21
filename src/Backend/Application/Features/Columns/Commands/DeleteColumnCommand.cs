using Application.Interfaces;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record DeleteColumnCommand(Guid BoardId, Guid ColumnId) : IRequest;

public class DeleteColumnCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteColumnCommand>
{
    public async Task Handle(DeleteColumnCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageColumnsAsync(request.BoardId, ct);

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

        columnRepository.Delete(column);
        await unitOfWork.SaveChangesAsync(ct);

        await taskRepository.SoftDeleteTasksAndRelationsByColumnIdAsync(column.Id, ct);

        await columnRepository.DecrementPositionsAsync(request.BoardId, positionToShift, ct);
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