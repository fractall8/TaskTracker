using Application.Interfaces;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.Comments.Commands;

public record DeleteCommentCommand(Guid BoardId, Guid TaskId, Guid CommentId) : IRequest;

public class DeleteCommentCommandHandler(
    IBoardAccessService boardAccessService,
    ICommentRepository commentRepository, 
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUnitOfWork unitOfWork) 
    : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var comment = await commentRepository.GetByIdAsync(request.CommentId, ct);
        if (comment == null || comment.TaskId != request.TaskId)
            throw new KeyNotFoundException("Comment not found.");

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u.Id, ct);
        
        if (comment.CreatedById != currentUserId)
            throw new UnauthorizedAccessException("You can only delete your own comments.");

        commentRepository.Delete(comment);
        await unitOfWork.SaveChangesAsync(ct);
    }
}