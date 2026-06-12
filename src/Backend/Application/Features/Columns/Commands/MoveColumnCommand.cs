using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Authorization;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record MoveColumnCommand(Guid BoardId, Guid ColumnId, int NewPosition) : IRequest;

public class MoveColumnCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MoveColumnCommand>
{
    public async Task Handle(MoveColumnCommand request, CancellationToken ct)
    {
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);
        if (board is null)
        {
            throw new KeyNotFoundException($"Board {request.BoardId} does not exist");
        }

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => (Guid?)u.Id, ct);

        if (currentUserId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userRole = await boardRepository.GetUserRoleAsync(request.BoardId, currentUserId.Value, ct);

        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this board.");
        }

        if (!BoardRolePermissions.CanManageColumns(userRole.Value))
        {
            throw new UnauthorizedAccessException("You don't have permission to manage columns in this board.");
        }

        var column = await columnRepository.GetByIdAsync(request.ColumnId, ct);
        if (column is null || column.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException($"Column {request.ColumnId} does not exist on this board");
        }

        if (column.Position == request.NewPosition)
        {
            return;
        }

        var oldPosition = column.Position;
        await columnRepository.UpdatePositionsOnMoveAsync(request.BoardId, oldPosition, request.NewPosition, ct);

        column.Position = request.NewPosition;
        columnRepository.Update(column);
        
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class MoveColumnCommandValidator : AbstractValidator<MoveColumnCommand>
{
    public MoveColumnCommandValidator() {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Column ID is required.");

        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(0).WithMessage("New position must be greater than or equal to 0.");
    }
}