using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Authorization;
using Domain.Constants;
using FluentValidation;
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

        var userRole = await boardRepository.GetUserRoleAsync(request.BoardId, currentUserId.Value, ct);
        
        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this board.");
        }
        
        if (!BoardRolePermissions.CanEditBoard(userRole.Value))
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
            CreatedAt: board.CreatedAt,
            Role: (Contracts.Enums.BoardRoleDto)userRole.Value
        );
    }
}

public class UpdateBoardCommandValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardCommandValidator()
    {
        RuleFor(v => v.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(BoardConstants.MaxNameLength).WithMessage("Board name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(BoardConstants.MaxDescriptionLength).WithMessage("Description must not exceed 500 characters.");
    }
}