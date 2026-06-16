using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Comments.Queries;

public record GetCommentsByTaskIdQuery(Guid BoardId, Guid TaskId) : IRequest<List<CommentDto>>;

public class GetCommentsByTaskIdQueryHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    ICommentRepository commentRepository) 
    : IRequestHandler<GetCommentsByTaskIdQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetCommentsByTaskIdQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
            throw new KeyNotFoundException("Task not found on this board.");

        var comments = await commentRepository.GetByTaskIdAsync(request.TaskId, ct);

        return comments.Select(c => new CommentDto(
            c.Id, c.Text, c.TaskId, c.CreatedAt, c.CreatedById, c.UpdatedAt
        )).ToList();
    }
}

public class GetCommentsByTaskIdQueryValidator : AbstractValidator<GetCommentsByTaskIdQuery>
{
    public GetCommentsByTaskIdQueryValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");
    }
}