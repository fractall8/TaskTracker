using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
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
    IColumnRepository columnRepository,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);
        
        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);

        if (column == null)
        {
            throw new KeyNotFoundException("Column not found.");
        }

        if (column.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Column not found on this board.");
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
        
        return new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.ColumnId, task.AssigneeId, task.ReporterId, new());
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
            .MaximumLength(TaskItemConstants.MaxTitleLength).WithMessage($"Task title must not exceed {TaskItemConstants.MaxTitleLength} characters.");
            
        RuleFor(x => x.Description)
            .MaximumLength(TaskItemConstants.MaxDescriptionLength).WithMessage($"Description must not exceed {TaskItemConstants.MaxDescriptionLength} characters.");

    }
}