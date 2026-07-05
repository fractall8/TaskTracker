using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record AddBoardMemberCommand(
    Guid BoardId,
    Guid WorkspaceMemberId,
    BoardRole Role) : IRequest<Unit>;

public class AddBoardMemberCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IBoardRepository boardRepository,
    IRepository<BoardMember, Guid> boardMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddBoardMemberCommand, Unit>
{
    public async Task<Unit> Handle(AddBoardMemberCommand request, CancellationToken ct)
    {
        var board = await boardRepository.GetByIdAsync(request.BoardId, ct);

        if (board == null)
        {
            throw new KeyNotFoundException("Board not found.");
        }

        await workspaceAccessService.EnsureCanManageBoardMembersAsync(board.WorkspaceId, ct);

        var isAlreadyMember = await boardMemberRepository.AnyAsync(
            m => m.BoardId == request.BoardId && m.WorkspaceMemberId == request.WorkspaceMemberId,
            ct);

        if (isAlreadyMember)
        {
            throw new InvalidOperationException("This user is already a member of this board.");
        }

        var boardMember = new BoardMember
        {
            Id = Guid.NewGuid(),
            BoardId = request.BoardId,
            WorkspaceMemberId = request.WorkspaceMemberId,
            Role = request.Role,
            JoinedAt = DateTimeOffset.UtcNow
        };

        await boardMemberRepository.AddAsync(boardMember, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

public class AddBoardMemberCommandValidator : AbstractValidator<AddBoardMemberCommand>
{
    public AddBoardMemberCommandValidator()
    {
        RuleFor(v => v.BoardId).NotEmpty();
        RuleFor(v => v.WorkspaceMemberId).NotEmpty();
        RuleFor(v => v.Role).IsInEnum().WithMessage("Invalid board role.");
    }
}
