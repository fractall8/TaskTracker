using Application.Features.Boards.Commands;
using FluentValidation;

namespace Application.Features.Boards.Validators;

public class UpdateBoardCommandValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardCommandValidator()
    {
        RuleFor(v => v.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(100).WithMessage("Board name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}