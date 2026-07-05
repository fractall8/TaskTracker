using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.Comments.Commands;

public record UpdateCommentCommand(Guid BoardId, Guid TaskId, Guid CommentId, string Text) : IRequest<CommentDto>;

public class UpdateCommentCommandHandler(
    IBoardAccessService boardAccessService,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    ICommentRepository commentRepository,

    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanManageCommentsAsync(request.BoardId, cancellationToken);

        var comment = await commentRepository.GetCommentWithDetailsAsync(request.CommentId, cancellationToken);

        if (comment == null || comment.Task?.Id != request.TaskId || comment.Task?.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Comment not found or does not belong to this task/board.");
        }

        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new { u.Id, u.DisplayName, u.AvatarUrl },
            cancellationToken);

        if (userInfo == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        if (comment.CreatedById != userInfo.Id)
        {
            throw new UnauthorizedAccessException("You can only edit your own comments.");
        }

        comment.Text = request.Text;

        commentRepository.Update(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CommentDto(
            Id: comment.Id,
            Text: comment.Text,
            TaskId: comment.TaskId,
            CreatedAt: comment.CreatedAt,
            UpdatedAt: comment.UpdatedAt,
            AuthorId: comment.CreatedById!.Value,
            AuthorAvatarUrl: userInfo.AvatarUrl,
            AuthorName: userInfo.DisplayName ?? string.Empty);
    }
}

public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty().WithMessage("Board ID is required.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.CommentId).NotEmpty().WithMessage("Comment ID is required.");
        RuleFor(x => x.Text).NotEmpty().MaximumLength(CommentConstants.MaxTextLength);
    }
}
