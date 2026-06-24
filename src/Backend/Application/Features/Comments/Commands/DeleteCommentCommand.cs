using Application.Interfaces;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Comments.Commands;

public record DeleteCommentCommand(Guid BoardId, Guid TaskId, Guid CommentId) : IRequest;

public class DeleteCommentCommandHandler(
    IBoardAccessService boardAccessService,
    ICommentRepository commentRepository,
    ITaskRepository taskRepository,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageCommentsAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }

        var comment = await commentRepository.GetByIdAsync(request.CommentId, ct);
        if (comment == null || comment.TaskId != request.TaskId)
        {
            throw new KeyNotFoundException("Comment not found.");
        }

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u.Id, ct);

        if (comment.CreatedById != currentUserId)
        {
            throw new UnauthorizedAccessException("You can only delete your own comments.");
        }

        commentRepository.Delete(comment);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(x => x.CommentId)
            .NotEmpty().WithMessage("Comment ID is required.");
    }
}
