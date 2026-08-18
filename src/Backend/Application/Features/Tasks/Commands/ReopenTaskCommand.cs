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

public record ReopenTaskCommand(Guid BoardId, Guid TaskId) : IRequest<TaskDto>;

public class ReopenTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReopenTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(ReopenTaskCommand request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanCompleteTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct)
                   ?? throw new NotFoundException("Task not found.");

        // Without this, holding a role on one board would authorise reopening a task on another.
        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found.");
        }

        if (task.IsCompleted)
        {
            task.IsCompleted = false;
            task.CompletedAt = null;
            task.CompletedById = null;

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

public class ReopenTaskCommandValidator : AbstractValidator<ReopenTaskCommand>
{
    public ReopenTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
