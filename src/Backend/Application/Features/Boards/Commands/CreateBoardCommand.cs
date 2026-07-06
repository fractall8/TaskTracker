using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record CreateBoardCommand(Guid WorkspaceId, string Name, string? Description) : IRequest<BoardDto>;

public class CreateBoardCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(CreateBoardCommand request, CancellationToken ct)
    {
        var userInfo = await workspaceAccessService.EnsureCanManageWorkspaceAsync(request.WorkspaceId, ct);

        var workspaceMember =
            await workspaceMemberRepository.GetByWorkspaceAndUserIdAsync(request.WorkspaceId, userInfo.UserId, ct);

        if (workspaceMember == null)
        {
            throw new InvalidOperationException("User is not a member of this workspace.");
        }

        var workspaceAdminsAndOwners = await workspaceMemberRepository.FindAsync(
            m => m.WorkspaceId == request.WorkspaceId &&
                 (m.Role == WorkspaceRole.Owner || m.Role == WorkspaceRole.Admin),
            ct);

        if (!workspaceAdminsAndOwners.Any())
        {
            throw new InvalidOperationException("Workspace has no owner or admins.");
        }

        var board = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var member in workspaceAdminsAndOwners)
        {
            var boardMember = new BoardMember
            {
                Id = Guid.NewGuid(),
                BoardId = board.Id,
                WorkspaceMemberId = member.Id,
                Role = BoardRole.Admin,
                JoinedAt = DateTimeOffset.UtcNow
            };

            board.Members.Add(boardMember);
        }
        await boardRepository.AddAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new BoardDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt,
            UserRole: Contracts.Enums.BoardRoleDto.Admin,
            Members: [new UserWithRoleDto(userInfo.UserId, userInfo.Email, null, Contracts.Enums.BoardRoleDto.Admin)],
            Columns: []
        );
    }
}

public class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(BoardConstants.MaxNameLength).WithMessage($"Board name must not exceed {BoardConstants.MaxNameLength} characters.");

        RuleFor(v => v.Description)
            .MaximumLength(BoardConstants.MaxDescriptionLength).WithMessage($"Description must not exceed {BoardConstants.MaxDescriptionLength} characters.");
    }
}
