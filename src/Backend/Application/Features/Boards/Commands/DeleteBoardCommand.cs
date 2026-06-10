using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record DeleteBoardCommand(Guid BoardId) : IRequest;

public class DeleteBoardCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteBoardCommand>
{
    private readonly List<BoardRole> _allowedRoles = [BoardRole.Admin];
    
    public async Task Handle(DeleteBoardCommand request, CancellationToken ct)
    {
        var currentUserId =
            await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => (Guid?)u.Id, ct);

        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userRole = await boardRepository.GetUserRoleAsync(request.BoardId, currentUserId.Value, ct);

        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this board.");
        }
        
        if (!_allowedRoles.Contains(userRole.Value))
        {
            throw new UnauthorizedAccessException("You don't have permission to edit this board.");
        }

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct)
                    ?? throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");

        boardRepository.Delete(board);

        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class DeleteBoardCommandValidator : AbstractValidator<DeleteBoardCommand>
{
    public DeleteBoardCommandValidator()
    {
        RuleFor(v => v.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");
    }
}