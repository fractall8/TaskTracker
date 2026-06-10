using Application.Features.Boards.Queries;
using FluentValidation;

namespace Application.Features.Boards.Validators;

public class GetBoardsQueryValidator : AbstractValidator<GetBoardsQuery>
{
    public GetBoardsQueryValidator()
    {
        RuleFor(v => v.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(v => v.SearchTerm)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");
    }
}