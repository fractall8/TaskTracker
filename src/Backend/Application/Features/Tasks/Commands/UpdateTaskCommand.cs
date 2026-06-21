using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record UpdateTaskCommand(
    Guid BoardId,
    Guid TaskId,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId,
    Guid ColumnId) : IRequest<TaskDto>;

public class UpdateTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

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

        if (task.ColumnId != request.ColumnId)
        {
            var targetColumn = await columnRepository.GetByIdAsync(request.ColumnId, ct);

            if (targetColumn == null)
            {
                throw new Exception("Target column not found.");
            }

            if (targetColumn.BoardId != request.BoardId)
            {
                throw new Exception("Target column not found on this board.");
            }

            await taskRepository.DecrementPositionsAsync(task.ColumnId, task.Position + 1, ct);

            var maxPosition = await taskRepository.GetMaxPositionAsync(request.ColumnId, ct);

            task.ColumnId = request.ColumnId;
            task.Position = maxPosition + 1;
        }

        taskRepository.Update(task);
        await unitOfWork.SaveChangesAsync(ct);

        await taskRepository.LoadUsersForTaskAsync(task, ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.ColumnId, task.AssigneeId, task.Assignee?.DisplayName, task.ReporterId, task.Reporter?.DisplayName,
            []);
    }
}

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();

        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.ColumnId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(TaskItemConstants.MaxTitleLength)
            .WithMessage($"Task title must not exceed {TaskItemConstants.MaxTitleLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(TaskItemConstants.MaxDescriptionLength)
            .WithMessage($"Description must not exceed {TaskItemConstants.MaxDescriptionLength} characters.");
    }
}