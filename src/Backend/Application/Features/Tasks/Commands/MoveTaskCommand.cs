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
    IBoardActionNotifier  boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MoveTaskCommand>
{
    public async Task Handle(MoveTaskCommand request, CancellationToken ct)
    {
        var boardAccessContext = await accessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var taskToMove = await taskRepository.GetTaskWithColumnAsync(request.TaskId, ct);

        if (taskToMove == null)
        {
            throw new NotFoundException("Task not found.");
        }

        if (taskToMove.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found on this board.");
        }

        var targetColumn = await columnRepository.GetByIdAsync(request.TargetColumnId, ct);

        if (targetColumn == null || targetColumn.BoardId != request.BoardId)
        {
            throw new NotFoundException("Target column not found on this board.");
        }

        var oldColumnId = taskToMove.ColumnId;
        var oldPosition = taskToMove.Position;

        var targetColumnMaxPosition = await taskRepository.GetMaxPositionAsync(request.TargetColumnId, ct);
        var isSameColumnMove = oldColumnId == request.TargetColumnId;
        int safeNewPosition;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (isSameColumnMove)
            {
                safeNewPosition = Math.Min(request.NewPosition, targetColumnMaxPosition);

                if (oldPosition != safeNewPosition)
                {
                    await taskRepository.UpdatePositionsOnMoveAsync(oldColumnId, oldPosition, safeNewPosition, token);
                }
            }
            else
            {
                var maxAllowedPosition = targetColumnMaxPosition + 1;
                safeNewPosition = Math.Min(request.NewPosition, maxAllowedPosition);

                await taskRepository.DecrementPositionsAsync(oldColumnId, oldPosition + 1, token);
                await taskRepository.IncrementPositionsAsync(request.TargetColumnId, safeNewPosition, token);

                taskToMove.ColumnId = request.TargetColumnId;
            }

            if (oldPosition != safeNewPosition || !isSameColumnMove)
            {
                taskToMove.Position = safeNewPosition;
                taskRepository.Update(taskToMove);
                await unitOfWork.SaveChangesAsync(token);
            }
        }, ct);

        if (isSameColumnMove && oldPosition == taskToMove.Position)
        {
            return;
        }

        var updatedSourceColumnTasks = await taskRepository.GetTasksByColumnIdAsync(oldColumnId, ct);
        var updatedTargetColumnTasks = isSameColumnMove
            ? updatedSourceColumnTasks
            : await taskRepository.GetTasksByColumnIdAsync(request.TargetColumnId, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TasksReordered,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TasksReorderedPayload(
                request.TaskId,
                oldColumnId,
                request.TargetColumnId,
                taskToMove.Position,
                BoardActionPositionMappings.ToTaskPositions(updatedSourceColumnTasks),
                BoardActionPositionMappings.ToTaskPositions(updatedTargetColumnTasks))), ct);
    }
}

public class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskCommandValidator()
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
