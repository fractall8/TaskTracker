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

public record DetachTagFromTaskCommand(Guid BoardId, Guid TaskId, Guid TagId) : IRequest<TaskDto>;

public class DetachTagFromTaskCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    ITagRepository tagRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DetachTagFromTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(DetachTagFromTaskCommand request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanTagTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct)
                   ?? throw new NotFoundException("Task not found.");

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found on this board.");
        }

        if (await tagRepository.GetLinkAsync(task.Id, request.TagId, ct) is { } link)
        {
            tagRepository.RemoveLink(link);
            await unitOfWork.SaveChangesAsync(ct);

            task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct)
                   ?? throw new NotFoundException("Task not found.");

            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskTagsChanged,
                access.UserId,
                dateTimeProvider.UtcNow,
                new TaskTagsChangedPayload(task.Id, task.ToTagDtos())), ct);
        }

        return task.ToDto();
    }
}

public class DetachTagFromTaskCommandValidator : AbstractValidator<DetachTagFromTaskCommand>
{
    public DetachTagFromTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
    }
}
