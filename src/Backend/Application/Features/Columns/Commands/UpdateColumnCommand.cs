using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record UpdateColumnCommand(Guid BoardId, Guid ColumnId, string Name) : IRequest<ColumnDto>;

public class UpdateColumnCommandHandler(
    IBoardAccessService boardAccessService,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateColumnCommand, ColumnDto>
{
    public async Task<ColumnDto> Handle(UpdateColumnCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageColumnsAsync(request.BoardId, ct);

        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);

        if (column == null)
        {
            throw new KeyNotFoundException($"Column {request.ColumnId} does not exist");
        }

        if (column.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Column not found on this board.");
        }

        if (!string.Equals(column.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existingNames = await columnRepository.GetNameListByBoardIdAsync(column.BoardId, ct);

            if (existingNames.Any(existingName =>
                    string.Equals(existingName, request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Column name already exists");
            }

            column.Name = request.Name;
        }

        columnRepository.Update(column);
        await unitOfWork.SaveChangesAsync(ct);

        return new ColumnDto(
            column.Id,
            column.Name,
            column.Position);
    }
}

public class UpdateColumnCommandValidator : AbstractValidator<UpdateColumnCommand>
{
    public UpdateColumnCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("BoardId is required.");

        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Column ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(ColumnConstants.MaxNameLength).WithMessage("Column name must not exceed 50 characters.");
    }
}