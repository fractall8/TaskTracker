using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Commands;

public record UpdateTaskDueDateCommand(Guid BoardId, Guid TaskId, DateTimeOffset? DueDate) : IRequest<TaskDto>;

public class UpdateTaskDueDateCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTaskDueDateCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskDueDateCommand request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, cancellationToken);

        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

        if (task == null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        task.DueDate = request.DueDate;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var attachments = task.Attachments?.Select(a => new AttachmentDto(
            a.Id, a.FileName, a.FileUrl, a.SizeInBytes, a.CreatedAt, a.CreatedById)).ToList() ?? [];

        return new TaskDto
        (
            task.Id,
            task.Title,
            task.Description,
            task.Position,
            task.DueDate,
            task.ColumnId,
            task.AssigneeId,
            task.Assignee?.DisplayName,
            task.Assignee?.AvatarUrl,
            task.ReporterId,
            task.Reporter?.DisplayName,
            task.Reporter?.AvatarUrl,
            attachments
        );
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
