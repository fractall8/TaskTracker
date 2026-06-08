using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Features.Boards.Commands;

public record UpdateBoardCommand(Guid BoardId, string Name, string? Description) : IRequest<BoardPreviewDto>;

public class UpdateBoardCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBoardCommand, BoardPreviewDto>
{
    public async Task<BoardPreviewDto> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => (Guid?)u.Id, ct);

        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board == null)
        {
            throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");
        }

        var hasRequiredRole = await boardRepository.HasRoleAsync(request.BoardId, currentUserId.Value, ct, BoardRole.Admin,
            BoardRole.ScrumMaster);

        if (!hasRequiredRole)
        {
            throw new UnauthorizedAccessException("You don't have permission to edit this board.");
        }
        
        board.Name = request.Name;
        board.Description = request.Description;
        
        await unitOfWork.SaveChangesAsync(ct);
        
        return new BoardPreviewDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt
        );
    }
}