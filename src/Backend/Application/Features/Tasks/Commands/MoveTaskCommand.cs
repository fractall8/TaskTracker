using Application.Interfaces;
using Application.Interfaces.Services;
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

        if (oldColumnId == request.TargetColumnId)
        {
            if (oldPosition == request.NewPosition) return;

            await taskRepository.UpdatePositionsOnMoveAsync(oldColumnId, oldPosition, request.NewPosition, ct);
        }
        else
        {
            await taskRepository.DecrementPositionsAsync(oldColumnId, oldPosition + 1, ct);

            await taskRepository.IncrementPositionsAsync(request.TargetColumnId, request.NewPosition, ct);

            taskToMove.ColumnId = request.TargetColumnId;
        }

        taskToMove.Position = request.NewPosition;

        taskRepository.Update(taskToMove);
        await unitOfWork.SaveChangesAsync(ct);
    }
}