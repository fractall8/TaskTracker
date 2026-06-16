using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record CreateColumnCommand(Guid BoardId, string Name) : IRequest<ColumnDto>;

public class CreateColumnCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateColumnCommand, ColumnDto>
{
    public async Task<ColumnDto> Handle(CreateColumnCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageColumnsAsync(request.BoardId, ct);
        
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board is null)
        {
            throw new KeyNotFoundException($"Board {request.BoardId} does not exist");
        }
        
        var existingNamesEnumerable = await columnRepository.GetNameListByBoardIdAsync(request.BoardId, ct);
        var existingNames = existingNamesEnumerable.ToList();
        
        if (existingNames.Any(existingName =>
                string.Equals(existingName, request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Column name already exists");
        }

        var column = new Column
        {
            BoardId = request.BoardId,
            Name = request.Name,
            Position = existingNames.Count
        };

        await columnRepository.AddAsync(column, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return new ColumnDto(
            column.Id,
            column.Name,
            column.Position);
    }
}

public class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
{
    public CreateColumnCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(ColumnConstants.MaxNameLength).WithMessage("Column name must not exceed 50 characters.");
    }
}