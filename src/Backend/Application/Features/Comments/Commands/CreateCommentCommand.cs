using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
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
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageCommentsAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        var userInfo =
            await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => new { u.Id, u.DisplayName, u.AvatarUrl }, ct);

        if (userInfo == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = userInfo.Id,
        };

        await commentRepository.AddAsync(comment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CommentDto(
            Id: comment.Id,
            Text: comment.Text,
            TaskId: comment.TaskId,
            CreatedAt: comment.CreatedAt,
            AuthorId: comment.CreatedById.Value,
            AuthorName: userInfo.DisplayName ?? string.Empty,
            AuthorAvatarUrl: userInfo.AvatarUrl,
            UpdatedAt: comment.UpdatedAt);
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
