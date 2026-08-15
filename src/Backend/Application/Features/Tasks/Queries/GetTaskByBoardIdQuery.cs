using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Queries;

public record GetTasksByBoardIdQuery(Guid BoardId) : IRequest<List<TaskDto>>;

public class GetTasksByBoardIdQueryHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository)
    : IRequestHandler<GetTasksByBoardIdQuery, List<TaskDto>>
{
    public async Task<List<TaskDto>> Handle(GetTasksByBoardIdQuery request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, cancellationToken);

        var tasks = await taskRepository.GetTasksByBoardIdAsync(request.BoardId, cancellationToken);

        return [.. tasks.Select(task => new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.IsCompleted, task.CompletedAt,
            task.ColumnId, task.AssigneeId, task.Assignee?.DisplayName, task.Assignee?.AvatarUrl, task.ReporterId,
            task.Reporter?.DisplayName,
            task.Reporter?.AvatarUrl,
            []))];
    }
}

public class GetTasksByBoardIdQueryValidator : AbstractValidator<GetTasksByBoardIdQuery>
{
    public GetTasksByBoardIdQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
