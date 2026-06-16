using Application.Interfaces;
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
    public async Task<List<TaskDto>> Handle(GetTasksByBoardIdQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var tasks = await taskRepository.GetTasksByBoardIdAsync(request.BoardId, ct);

        return tasks.Select(task => new TaskDto(
            task.Id, task.Title, task.Description, task.Position, task.DueDate,
            task.ColumnId, task.AssigneeId, task.ReporterId, []
        )).ToList();
    }
}

public class GetTasksByBoardIdQueryValidator : AbstractValidator<GetTasksByBoardIdQuery>
{
    public GetTasksByBoardIdQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}