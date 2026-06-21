using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record UpdateBoardCommand(Guid BoardId, string Name, string? Description) : IRequest<BoardPreviewDto>;

public class UpdateBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBoardCommand, BoardPreviewDto>
{
    public async Task<BoardPreviewDto> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanEditBoardAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board == null)
        {
            throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");
        }

        board.Name = request.Name;
        board.Description = request.Description;

        await unitOfWork.SaveChangesAsync(ct);

        return new BoardPreviewDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt,
            Role: (Contracts.Enums.BoardRoleDto)boardAccessContext.Role
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
            .MaximumLength(BoardConstants.MaxNameLength).WithMessage($"Board name must not exceed {BoardConstants.MaxNameLength} characters.");

        RuleFor(v => v.Description)
            .MaximumLength(BoardConstants.MaxDescriptionLength).WithMessage($"Description must not exceed {BoardConstants.MaxDescriptionLength} characters.");
    }
}