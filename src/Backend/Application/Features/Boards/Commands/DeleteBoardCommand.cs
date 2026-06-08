using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;

namespace Application.Features.Boards.Commands;

public record DeleteBoardCommand(Guid BoardId) : IRequest;

public class DeleteBoardCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteBoardCommand>
{
    public async Task Handle(DeleteBoardCommand request, CancellationToken ct)
    {
        var currentUserId =
            await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => (Guid?)u.Id, ct);

        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var hasAdminRole = await boardRepository.HasRoleAsync(request.BoardId, currentUserId.Value, ct, BoardRole.Admin);

        if (!hasAdminRole)
        {
            throw new UnauthorizedAccessException("You don't have access to this board or you are not an Admin.");
        }

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct)
                    ?? throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");

        boardRepository.Delete(board);

        await unitOfWork.SaveChangesAsync(ct);
    }
}