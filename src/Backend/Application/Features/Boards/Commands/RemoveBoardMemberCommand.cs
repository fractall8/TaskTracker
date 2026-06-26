using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record RemoveBoardMemberCommand(Guid BoardId, Guid WorkspaceMemberId) : IRequest<Unit>;

public class RemoveBoardMemberCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IBoardRepository boardRepository,
    IRepository<BoardMember, Guid> boardMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveBoardMemberCommand, Unit>
{
    public async Task<Unit> Handle(RemoveBoardMemberCommand request, CancellationToken ct)
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

        if (boardMember.Role == BoardRole.Admin)
        {
            var totalAdmins = await boardMemberRepository.CountAsync(
                m => m.BoardId == request.BoardId && m.Role == BoardRole.Admin,
                ct);

            if (totalAdmins <= 1)
            {
                throw new InvalidOperationException("Cannot remove the last Admin from this board.");
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
