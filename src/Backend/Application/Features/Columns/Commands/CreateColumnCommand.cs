using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Features.Columns.Commands;

public record CreateColumnCommand(Guid BoardId, string Name) : IRequest<ColumnDto>;

public class CreateColumnCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    IWorkspaceLimitService workspaceLimitService,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateColumnCommand, ColumnDto>
{
    public async Task<ColumnDto> Handle(CreateColumnCommand request, CancellationToken ct)
    {
        var boardAccessContext = await boardAccessService.EnsureCanManageColumnsAsync(request.BoardId, ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board is null)
        {
            throw new NotFoundException($"Board {request.BoardId} does not exist");
        }

        Column column = null!;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.AcquireDistributedLockAsync($"board:{request.BoardId}:columns", token);

            await workspaceLimitService.EnsureCanAddColumnAsync(request.BoardId, board.WorkspaceId, token);

            var existingNamesEnumerable = await columnRepository.GetNameListByBoardIdAsync(request.BoardId, token);
            var existingNames = existingNamesEnumerable.ToList();

            if (existingNames.Any(existingName =>
                    string.Equals(existingName, request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException([
                    new ValidationFailure("Name", "A column with this name already exists on the board.")
                ]);
            }

            column = new Column
            {
                BoardId = request.BoardId,
                Name = request.Name,
                Position = existingNames.Count
            };

            await columnRepository.AddAsync(column, token);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.ColumnCreated,
            boardAccessContext.UserId,
            dateTimeProvider.UtcNow,
            new ColumnCreatedPayload(column.Id, column.Name, column.Position)), ct);

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
