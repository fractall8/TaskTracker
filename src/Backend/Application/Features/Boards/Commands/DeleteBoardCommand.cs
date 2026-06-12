using Application.Interfaces;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record DeleteBoardCommand(Guid BoardId) : IRequest;

public class DeleteBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteBoardCommand>
{
    public async Task Handle(DeleteBoardCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanDeleteBoardAsync(request.BoardId, ct);

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