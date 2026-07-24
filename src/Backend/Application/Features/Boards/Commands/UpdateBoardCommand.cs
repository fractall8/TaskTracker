using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Enums;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Constants;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record UpdateBoardCommand(Guid BoardId, string Name, string? Description) : IRequest<BoardPreviewDto>;

public class UpdateBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBoardCommand, BoardPreviewDto>
{
    public async Task<BoardPreviewDto> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanEditBoardAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board == null)
        {
            throw new NotFoundException($"Board with ID {request.BoardId} not found.");
        }

        board.Name = request.Name;
        board.Description = request.Description;

        await unitOfWork.SaveChangesAsync(ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            board.Id,
            BoardActionNotificationType.BoardRenamed,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new BoardRenamedPayload(request.Name)), ct);

        return new BoardPreviewDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt,
            BoardRole: (BoardRoleDto)boardAccessContext.Role,
            IsArchived: board.IsArchived
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
