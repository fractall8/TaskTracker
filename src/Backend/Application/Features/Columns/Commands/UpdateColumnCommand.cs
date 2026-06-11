using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record UpdateColumnCommand(Guid ColumnId, string Name) : IRequest<ColumnDto>;

public class UpdateColumnCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateColumnCommand, ColumnDto>
{
    private readonly List<BoardRole> _allowedRoles = [BoardRole.Admin, BoardRole.ScrumMaster];

    public async Task<ColumnDto> Handle(UpdateColumnCommand request, CancellationToken ct)
    {
        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);
        if (column is null)
        {
            throw new KeyNotFoundException($"Column {request.ColumnId} does not exist");
        }

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => (Guid?)u.Id, ct)
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var userRole = await boardRepository.GetUserRoleAsync(column.BoardId, currentUserId, ct)
            ?? throw new UnauthorizedAccessException("You are not a member of this board.");

        if (!_allowedRoles.Contains(userRole))
        {
            throw new UnauthorizedAccessException("You don't have permission to update columns in this board.");
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
        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Column ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(50).WithMessage("Column name must not exceed 50 characters.");
    }
}