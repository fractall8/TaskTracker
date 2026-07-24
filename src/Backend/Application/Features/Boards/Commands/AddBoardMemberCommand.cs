using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record AddBoardMemberCommand(
    Guid BoardId,
    Guid WorkspaceMemberId,
    BoardRole Role) : IRequest<Unit>;

public class AddBoardMemberCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceMemberRepository workspaceMemberRepository,
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
            throw new NotFoundException("Board not found.");
        }

        if (board.IsArchived)
        {
            throw new BusinessRuleValidationException("Cannot manage members on an archived board.");
        }

        await workspaceAccessService.EnsureCanManageBoardMembersAsync(board.WorkspaceId, ct);

        var targetMember = await workspaceMemberRepository.GetByIdAsync(request.WorkspaceMemberId, ct);

        if (targetMember == null || targetMember.WorkspaceId != board.WorkspaceId)
        {
            throw new BusinessRuleValidationException("The user is not a member of this workspace.");
        }

        var isAlreadyMember = await boardMemberRepository.AnyAsync(
            m => m.BoardId == request.BoardId && m.WorkspaceMemberId == request.WorkspaceMemberId,
            ct);

        if (isAlreadyMember)
        {
            throw new ConflictException("This user is already a member of this board.");
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
