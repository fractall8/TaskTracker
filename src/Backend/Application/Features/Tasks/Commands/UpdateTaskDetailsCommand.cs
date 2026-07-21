using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record UpdateTaskDetailsCommand(
    Guid BoardId,
    Guid TaskId,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId) : IRequest<TaskDto>;

public class UpdateTaskCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    ITaskRepository taskRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTaskDetailsCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskDetailsCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        if (request.AssigneeId.HasValue)
        {
            var assigneeRole = await boardRepository.GetUserRoleAsync(request.BoardId, request.AssigneeId.Value, ct);
            if (!assigneeRole.HasValue)
            {
                throw new InvalidOperationException("The selected user is not a physical member of this board.");
            }
        }

        var task = await taskRepository.GetTaskWithColumnAsync(request.TaskId, ct);

        if (task == null)
        {
            throw new Exception("Task not found.");
        }

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.AssigneeId = request.AssigneeId;

        taskRepository.Update(task);
        await unitOfWork.SaveChangesAsync(ct);

        await taskRepository.LoadUsersForTaskAsync(task, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskUpdated,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TaskUpdatedPayload(
                task.ColumnId,
                task.Id,
                task.Title,
                task.Description,
                task.AssigneeId)), ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskDetailsUpdated,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TaskDetailsUpdatedPayload(
                task.Id,
                task.Title,
                task.Description,
                task.AssigneeId,
                task.Assignee?.DisplayName,
                task.Assignee?.AvatarUrl
            )
        ), ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.ColumnId, task.AssigneeId, task.Assignee?.DisplayName, task.Assignee?.AvatarUrl, task.ReporterId,
            task.Reporter?.DisplayName,
            task.Reporter?.AvatarUrl,
            []);
    }
}

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskDetailsCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();

        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(TaskItemConstants.MaxTitleLength)
            .WithMessage($"Task title must not exceed {TaskItemConstants.MaxTitleLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(TaskItemConstants.MaxDescriptionLength)
            .WithMessage($"Description must not exceed {TaskItemConstants.MaxDescriptionLength} characters.");
    }
}
