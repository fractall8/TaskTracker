using Application.Common.Mappings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Tasks.Queries;

public record GetTaskByIdQuery(Guid BoardId, Guid TaskId) : IRequest<TaskDto>;

public class GetTaskByIdQueryHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository)
    : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, cancellationToken);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, cancellationToken);
        if (task == null)
        {
            throw new NotFoundException("Task not found.");
        }

        if (task.Column?.BoardId != request.BoardId)
        {
            throw new NotFoundException("Task not found on this board.");
        }

        return task.ToDto();
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
