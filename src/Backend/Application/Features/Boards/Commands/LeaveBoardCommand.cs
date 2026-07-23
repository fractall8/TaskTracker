using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Boards.Commands;

public record LeaveBoardCommand(Guid BoardId) : IRequest;

public class LeaveBoardCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IBoardRepository boardRepository,
    IBoardAccessService boardAccessService,
    IBoardMemberRepository boardMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LeaveBoardCommand>
{
    public async Task Handle(LeaveBoardCommand request, CancellationToken ct)
    {
        var (currentUserId, _) = await boardAccessService.GetCurrentUserAsync(ct);

        var board = await boardRepository.GetByIdAsync(request.BoardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        var workspaceRole = await workspaceRepository.GetUserRoleAsync(board.WorkspaceId, currentUserId, ct);

        if (workspaceRole == null)
        {
            throw new BusinessRuleValidationException("You are not a member of this workspace.");
        }

        if (workspaceRole == WorkspaceRole.Owner)
        {
            throw new BusinessRuleValidationException("As a Workspace Owner, you cannot leave the board.");
        }

        if (workspaceRole == WorkspaceRole.Member)
        {
            throw new BusinessRuleValidationException("You cannot voluntarily leave a board. Please ask a Board Admin to remove you.");
        }

        await boardMemberRepository.RemoveUserFromBoardAsync(request.BoardId, currentUserId, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
