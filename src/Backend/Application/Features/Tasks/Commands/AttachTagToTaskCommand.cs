using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Entities;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record AttachTagToTaskCommand(Guid BoardId, Guid TaskId, Guid TagId) : IRequest<TaskDto>;

public class AttachTagToTaskCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    ITaskRepository taskRepository,
    ITagRepository tagRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AttachTagToTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(AttachTagToTaskCommand request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanTagTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct)
                   ?? throw new NotFoundException("Task not found.");

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found on this board.");
        }

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        // A tag belongs to one workspace, so a tag from another tenant must not land on this task.
        var tag = await tagRepository.GetByIdInWorkspaceAsync(request.TagId, board.WorkspaceId, ct)
                  ?? throw new NotFoundException("Tag not found in this workspace.");

        if (await tagRepository.GetLinkAsync(task.Id, tag.Id, ct) is null)
        {
            await tagRepository.AddLinkAsync(new TaskTag { Id = Guid.NewGuid(), TaskId = task.Id, TagId = tag.Id }, ct);
            await unitOfWork.SaveChangesAsync(ct);

            task = await taskRepository.GetTaskWithDetailsNoTrackingAsync(request.TaskId, ct)
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

public class AttachTagToTaskCommandValidator : AbstractValidator<AttachTagToTaskCommand>
{
    public AttachTagToTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
    }
}
