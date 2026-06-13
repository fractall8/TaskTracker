using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Queries;

public record GetTaskByIdQuery(Guid BoardId, Guid TaskId) : IRequest<TaskDto>;

public class GetTaskByIdQueryHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository)
    : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }
        
        var attachments = task.Attachments.Select(a => new AttachmentDto(
            a.Id, a.FileName, a.FileUrl, a.SizeInBytes, a.CreatedAt, a.CreatedById)).ToList();

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.ColumnId, task.AssigneeId, task.ReporterId, attachments);
    }
}

public class GetTaskByIdQueryValidator : AbstractValidator<GetTaskByIdQuery>
{
    public GetTaskByIdQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}