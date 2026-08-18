using Application.Common.Mappings;
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

        // Attachments are not loaded by this query, so ToDto yields an empty list here as it did before.
        return [.. tasks.Select(task => task.ToDto())];
    }
}

public class GetTasksByBoardIdQueryValidator : AbstractValidator<GetTasksByBoardIdQuery>
{
    public GetTasksByBoardIdQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
