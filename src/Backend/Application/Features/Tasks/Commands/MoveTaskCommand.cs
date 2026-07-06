using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record MoveTaskCommand(
    Guid BoardId,
    Guid TaskId,
    Guid TargetColumnId,
    int NewPosition) : IRequest;

public class MoveTaskCommandHandler(
    IBoardAccessService accessService,
    ITaskRepository taskRepository,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MoveTaskCommand>
{
    public async Task Handle(MoveTaskCommand request, CancellationToken ct)
    {
        await accessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var taskToMove = await taskRepository.GetTaskWithColumnAsync(request.TaskId, ct);

        if (taskToMove?.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }

        var targetColumn = await columnRepository.GetByIdAsync(request.TargetColumnId, ct);

        if (targetColumn == null || targetColumn.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Target column not found on this board.");
        }

        var oldColumnId = taskToMove.ColumnId;
        var oldPosition = taskToMove.Position;

        var targetColumnMaxPosition = await taskRepository.GetMaxPositionAsync(request.TargetColumnId, ct);

        int safeNewPosition;

        if (oldColumnId == request.TargetColumnId)
        {
            safeNewPosition = Math.Min(request.NewPosition, targetColumnMaxPosition);

            if (oldPosition == safeNewPosition)
            {
                return;
            }

            await taskRepository.UpdatePositionsOnMoveAsync(oldColumnId, oldPosition, safeNewPosition, ct);
        }
        else
        {
            var maxAllowedPosition = targetColumnMaxPosition + 1;
            safeNewPosition = Math.Min(request.NewPosition, maxAllowedPosition);

            await taskRepository.DecrementPositionsAsync(oldColumnId, oldPosition + 1, ct);
            await taskRepository.IncrementPositionsAsync(request.TargetColumnId, safeNewPosition, ct);

            taskToMove.ColumnId = request.TargetColumnId;
        }

        taskToMove.Position = safeNewPosition;

        taskRepository.Update(taskToMove);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class MoveColumnCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveColumnCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(x => x.TargetColumnId)
            .NotEmpty().WithMessage("Target column ID is required.");

        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(0).WithMessage("New position must be greater than or equal to 0.");
    }
}
