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
    ICommentRepository commentRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetCommentsByTaskIdQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetCommentsByTaskIdQuery request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
            throw new KeyNotFoundException("Task not found on this board.");

        var comments = await commentRepository.GetByTaskIdAsync(request.TaskId, ct);
        if (!comments.Any())
            return new List<CommentDto>();

        var authorIds = comments
            .Where(c => c.CreatedById.HasValue)
            .Select(c => c.CreatedById!.Value)
            .Distinct()
            .ToList();

        var authors = await userRepository.GetByIdsAsync(authorIds, ct);
        var authorDictionary = authors.ToDictionary(a => a.Id, a => a);

        var commentsDtos = new List<CommentDto>();

        foreach (var comment in comments)
        {
            if (comment.CreatedById == null || !authorDictionary.TryGetValue(comment.CreatedById.Value, out var author))
                continue;

            commentsDtos.Add(new CommentDto(
                Id: comment.Id,
                Text: comment.Text,
                TaskId: comment.TaskId,
                CreatedAt: comment.CreatedAt,
                UpdatedAt: comment.UpdatedAt,
                AuthorId: comment.CreatedById.Value,
                AuthorName: author.DisplayName ?? string.Empty
            ));
        }

        return commentsDtos;
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