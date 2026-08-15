using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record UpdateTaskDueDateCommand(Guid BoardId, Guid TaskId, DateTimeOffset? DueDate) : IRequest<TaskDto>;

public class UpdateTaskDueDateCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTaskDueDateCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskDueDateCommand request, CancellationToken cancellationToken)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, cancellationToken);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, cancellationToken);

        if (task == null)
        {
            throw new NotFoundException("Task not found.");
        }

        // Without this, holding a role on one board would authorise editing a task on another.
        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found on this board.");
        }

        task.DueDate = request.DueDate;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskDueDateUpdated,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TaskDueDateUpdatedPayload(task.Id, task.DueDate)
        ), cancellationToken);

        return task.ToDto();
    }
}

public class UpdateTaskDueDateCommandValidator : AbstractValidator<UpdateTaskDueDateCommand>
{
    public UpdateTaskDueDateCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();

        RuleFor(x => x.TaskId).NotEmpty();
    }
}
