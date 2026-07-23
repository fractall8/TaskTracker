using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record CreateTaskCommand(
    Guid BoardId,
    Guid ColumnId,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId) : IRequest<TaskDto>;

public class CreateTaskCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    ITaskRepository taskRepository,
    IWorkspaceLimitService workspaceLimitService,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        await workspaceLimitService.EnsureCanAddTaskAsync(request.BoardId, ct);

        if (request.AssigneeId.HasValue)
        {
            var assigneeRole = await boardRepository.GetUserRoleAsync(request.BoardId, request.AssigneeId.Value, ct);
            if (!assigneeRole.HasValue)
            {
                throw new BusinessRuleValidationException("The selected user is not a physical member of this board.");
            }
        }

        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);

        if (column == null)
        {
            throw new NotFoundException("Column not found.");
        }

        if (column.BoardId != request.BoardId)
        {
            throw new NotFoundException("Column not found on this board.");
        }

        var maxPosition = await taskRepository.GetMaxPositionAsync(request.ColumnId, ct);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = request.ColumnId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            AssigneeId = request.AssigneeId,
            ReporterId = boardAccessContext.UserId,
            Position = maxPosition + 1
        };

        await taskRepository.AddAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await taskRepository.LoadUsersForTaskAsync(task, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskCreated,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TaskCreatedPayload(
                request.ColumnId,
                task.Id,
                task.Title,
                task.Position,
                task.AssigneeId)), ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.ColumnId, task.AssigneeId, task.Assignee?.DisplayName, task.Assignee?.AvatarUrl, task.ReporterId,
            task.Reporter?.DisplayName,
            task.Reporter?.AvatarUrl,
            []);
    }
}

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();

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
