using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.Comments.Commands;

public record CreateCommentCommand(Guid BoardId, Guid TaskId, string Text) : IRequest<CommentDto>;

public class CreateCommentCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    ICommentRepository commentRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageCommentsAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        var user =
            await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u, ct);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = user.Id,
            AuthorId = user.Id,
            Author = user,
        };

        await commentRepository.AddAsync(comment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var commentDto = new CommentDto(
            Id: comment.Id,
            Text: comment.Text,
            TaskId: comment.TaskId,
            CreatedAt: comment.CreatedAt,
            AuthorId: comment.CreatedById.Value,
            AuthorName: user.DisplayName ?? string.Empty,
            AuthorAvatarUrl: user.AvatarUrl,
            UpdatedAt: comment.UpdatedAt);

        var commentsCount = await commentRepository.CountAsync(c => c.TaskId == request.TaskId, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskCommentsCountChanged,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new TaskCommentsCountChangedPayload(request.TaskId, commentsCount)), ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.CommentAdded,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new CommentAddedPayload(request.TaskId, commentDto)
        ), ct);

        return commentDto;
    }
}

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(x => x.Text).NotEmpty().MaximumLength(CommentConstants.MaxTextLength);
    }
}
