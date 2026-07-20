using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Authorization;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Hubs.Commands;

public record SubscribeBoardActionsCommand(Guid BoardId, Guid AzureAdObjectId) : IRequest;

public class SubscribeBoardActionsCommandHandler(
    IUserRepository userRepository,
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository)
    : IRequestHandler<SubscribeBoardActionsCommand>
{
    public async Task Handle(
        SubscribeBoardActionsCommand request,
        CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(request.AzureAdObjectId, u=> u, ct);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User is unauthorized");
        }

        var boardRole = await boardRepository.GetUserRoleAsync(request.BoardId,  user.Id, ct);

        if (boardRole == null)
        {
            throw new UnauthorizedAccessException("User is unauthorized");
        }

        if (BoardRolePermissions.CanViewBoard((BoardRole)boardRole))
        {
            throw new UnauthorizedAccessException("You cannot view the board");
        }

        var isArchivedBoard = await boardRepository.IsBoardArchivedAsync(request.BoardId, ct);

        if (isArchivedBoard)
        {
            throw new InvalidOperationException($"Board {request.BoardId} is archived");
        }
    }
}

public class SubscribeBoardActionsCommandValidator : AbstractValidator<SubscribeBoardActionsCommand>
{
    public SubscribeBoardActionsCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotNull();
    }
}
