using Application.Features.Boards.Commands;
using FluentValidation;

namespace Application.Features.Boards.Validators;

public class DeleteBoardCommandValidator : AbstractValidator<DeleteBoardCommand>
{
    public DeleteBoardCommandValidator()
    {
        RuleFor(v => v.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");
    }
}