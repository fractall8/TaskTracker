using Application.Interfaces;
using Application.Interfaces.Services;
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
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
            throw new KeyNotFoundException("Task not found.");

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u.Id, ct);
        
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = currentUserId,
        };

        await commentRepository.AddAsync(comment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CommentDto(comment.Id, comment.Text, comment.TaskId, comment.CreatedAt, comment.CreatedById, null);
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