using Application.Interfaces;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record DeleteTaskCommand(Guid BoardId, Guid TaskId) : IRequest;

public class DeleteTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, cancellationToken);

        var task = await taskRepository.GetTaskWithColumnAsync(request.TaskId, cancellationToken);

        if (task == null)
        {
            return;
        }

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }

        await taskRepository.DecrementPositionsAsync(task.ColumnId, task.Position + 1, cancellationToken);

        taskRepository.Delete(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
