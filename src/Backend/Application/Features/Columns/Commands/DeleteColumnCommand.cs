using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Authorization;
using FluentValidation;
using MediatR;

namespace Application.Features.Columns.Commands;

public record DeleteColumnCommand(Guid BoardId, Guid ColumnId) : IRequest;

public class DeleteColumnCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteColumnCommand>
{
    public async Task Handle(DeleteColumnCommand request, CancellationToken ct)
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

        var positionToShift = column.Position;
        
        // Will deal with tasks there later
        // TODO: deal with tasks in next pr
        columnRepository.Delete(column); 
        await unitOfWork.SaveChangesAsync(ct);

        await columnRepository.DecrementPositionsAsync(request.BoardId, positionToShift, ct);
    }
}

public class DeleteColumnCommandValidator : AbstractValidator<DeleteColumnCommand>
{
    public DeleteColumnCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Column ID is required.");
    }
}