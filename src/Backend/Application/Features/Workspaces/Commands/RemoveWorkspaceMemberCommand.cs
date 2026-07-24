using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record RemoveWorkspaceMemberCommand(Guid WorkspaceId, Guid UserIdToRemove) : IRequest;

public class RemoveWorkspaceMemberCommandHandler(
    IWorkspaceMemberRepository workspaceMemberRepository,
    IBoardMemberRepository boardMemberRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveWorkspaceMemberCommand>
{
    public async Task Handle(RemoveWorkspaceMemberCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanManageMembersAsync(request.WorkspaceId, cancellationToken);

        var targetMember = await workspaceMemberRepository.GetByWorkspaceAndUserIdAsync(
            request.WorkspaceId, request.UserIdToRemove, cancellationToken)
            ?? throw new BusinessRuleValidationException("User is not a member of this workspace.");

        var currentMember = await workspaceAccessService.GetCurrentUserInfoAsync(cancellationToken);

        if (currentMember.UserId == targetMember.UserId)
        {
            throw new BusinessRuleValidationException("You cannot remove yourself from the workspace using this command. Use 'Leave Workspace' instead.");
        }

        if (targetMember.Role == WorkspaceRole.Owner)
        {
            throw new BusinessRuleValidationException("The Owner cannot be removed from the workspace. Transfer ownership first.");
        }

        var userBoardMemberships = await boardMemberRepository.GetByWorkspaceMemberIdAsync(targetMember.Id, cancellationToken);
        foreach (var membership in userBoardMemberships)
        {
            boardMemberRepository.Delete(membership);
        }

        workspaceMemberRepository.Delete(targetMember);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
