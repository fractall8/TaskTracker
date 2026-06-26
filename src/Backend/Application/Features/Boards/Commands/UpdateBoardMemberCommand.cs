using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
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
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBoardMemberRoleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateBoardMemberRoleCommand request, CancellationToken ct)
    {
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board == null)
        {
            throw new KeyNotFoundException("Board not found.");
        }

        await workspaceAccessService.EnsureCanManageBoardMembersAsync(board.WorkspaceId, ct);

        var boardMember = await boardMemberRepository.GetAsync(
            m => m.BoardId == request.BoardId && m.WorkspaceMemberId == request.WorkspaceMemberId,
            ct) ?? throw new KeyNotFoundException("Board member not found.");

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
                throw new InvalidOperationException("Cannot demote the last Admin of this board.");
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
