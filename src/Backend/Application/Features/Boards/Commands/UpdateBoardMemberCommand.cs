using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Features.Boards.Commands;

public record UpdateBoardMemberRoleCommand(
    Guid BoardId,
    Guid WorkspaceMemberId,
    BoardRole NewRole) : IRequest<Unit>;

public class UpdateBoardMemberRoleCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IBoardRepository  boardRepository,
    IRepository<BoardMember, Guid> boardMemberRepository,
    IUserRepository  userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceRepository workspaceRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBoardMemberRoleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateBoardMemberRoleCommand request, CancellationToken ct)
    {
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board == null)
        {
            throw new NotFoundException("Board not found.");
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
            throw new BusinessRuleValidationException("You cannot change your own role on the board.");
        }

        if (initiatorWorkspaceRole != WorkspaceRole.Owner)
        {
            if (targetWorkspaceMember.Role == WorkspaceRole.Owner || targetWorkspaceMember.Role == WorkspaceRole.Admin)
            {
                throw new ForbiddenException("You can only change board roles for regular Workspace Members.");
            }
        }

        if ((targetWorkspaceMember.Role == WorkspaceRole.Owner || targetWorkspaceMember.Role == WorkspaceRole.Admin)
            && request.NewRole != BoardRole.Admin)
        {
            throw new BusinessRuleValidationException("Workspace Owners and Admins must always retain the Admin role on boards.");
        }

        var boardMember = await boardMemberRepository.GetAsync(
            m => m.BoardId == request.BoardId && m.WorkspaceMemberId == request.WorkspaceMemberId,
            ct) ?? throw new NotFoundException("Board member not found.");

        if (boardMember.Role == request.NewRole)
        {
            return Unit.Value;
        }

        if (boardMember.Role == BoardRole.Admin && request.NewRole != BoardRole.Admin)
        {
            var totalAdmins = await boardMemberRepository.CountAsync(
                m => m.BoardId == request.BoardId && m.Role == BoardRole.Admin,
                ct);

            if (totalAdmins <= 1)
            {
                throw new ValidationException([new ValidationFailure("Role", "Cannot demote the last Admin of this board.")]);
            }
        }

        boardMember.Role = request.NewRole;

        boardMemberRepository.Update(boardMember);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

public class UpdateBoardMemberRoleCommandValidator : AbstractValidator<UpdateBoardMemberRoleCommand>
{
    public UpdateBoardMemberRoleCommandValidator()
    {
        RuleFor(v => v.BoardId).NotEmpty();
        RuleFor(v => v.WorkspaceMemberId).NotEmpty();
        RuleFor(v => v.NewRole).IsInEnum().WithMessage("Invalid board role.");
    }
}
