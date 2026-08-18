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

public record CompleteTaskCommand(Guid BoardId, Guid TaskId) : IRequest<TaskDto>;

public class CompleteTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CompleteTaskCommand request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanCompleteTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct)
                   ?? throw new NotFoundException("Task not found.");

        // Without this, holding a role on one board would authorise completing a task on another.
        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found.");
        }

        if (!task.IsCompleted)
        {
            task.IsCompleted = true;
            task.CompletedAt = dateTimeProvider.UtcNow;
            task.CompletedById = access.UserId;

            await unitOfWork.SaveChangesAsync(ct);

            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskCompletionChanged,
                access.UserId,
                dateTimeProvider.UtcNow,
                new TaskCompletionChangedPayload(task.Id, task.IsCompleted, task.CompletedAt)), ct);
        }

        return task.ToDto();
    }
}

public class CompleteTaskCommandValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
