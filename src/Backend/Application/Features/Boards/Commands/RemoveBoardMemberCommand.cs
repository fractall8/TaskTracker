using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record RemoveBoardMemberCommand(Guid BoardId, Guid WorkspaceMemberId) : IRequest<Unit>;

public class RemoveBoardMemberCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceRepository workspaceRepository,
    IRepository<WorkspaceMember, Guid> workspaceMemberRepository,
    IBoardRepository boardRepository,
    IRepository<BoardMember, Guid> boardMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveBoardMemberCommand, Unit>
{
    public async Task<Unit> Handle(RemoveBoardMemberCommand request, CancellationToken ct)
    {
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        if (board.IsArchived)
        {
            throw new BusinessRuleValidationException("Cannot manage members on an archived board.");
        }

        await workspaceAccessService.EnsureCanManageBoardMembersAsync(board.WorkspaceId, ct);

        var initiatorId = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => u.Id,
            ct);

        var initiatorWorkspaceRole = await workspaceRepository.GetUserRoleAsync(board.WorkspaceId, initiatorId, ct);

        var targetWorkspaceMember = await workspaceMemberRepository.GetByIdAsync(request.WorkspaceMemberId, ct)
                                    ?? throw new NotFoundException("Workspace member not found.");

        if (targetWorkspaceMember.UserId == initiatorId)
        {
            throw new BusinessRuleValidationException("You cannot remove yourself from the board. Use the 'Leave Board' action instead.");
        }

        if (initiatorWorkspaceRole != WorkspaceRole.Owner)
        {
            if (targetWorkspaceMember.Role == WorkspaceRole.Owner || targetWorkspaceMember.Role == WorkspaceRole.Admin)
            {
                throw new ForbiddenException("You can only remove regular Workspace Members from the board.");
            }
        }

        var boardMember = await boardMemberRepository.GetAsync(
            m => m.BoardId == request.BoardId && m.WorkspaceMemberId == request.WorkspaceMemberId,
            ct) ?? throw new NotFoundException("Board member not found on this board.");

        if (boardMember.Role == BoardRole.Admin)
        {
            var totalAdmins = await boardMemberRepository.CountAsync(
                m => m.BoardId == request.BoardId && m.Role == BoardRole.Admin,
                ct);

            if (totalAdmins <= 1)
            {
                throw new BusinessRuleValidationException("Cannot remove the last Admin from this board.");
            }
        }

        boardMember.IsDeleted = true;
        boardMemberRepository.Update(boardMember);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

public class RemoveBoardMemberCommandValidator : AbstractValidator<RemoveBoardMemberCommand>
{
    public RemoveBoardMemberCommandValidator()
    {
        RuleFor(v => v.BoardId).NotEmpty();
        RuleFor(v => v.WorkspaceMemberId).NotEmpty();
    }
}
