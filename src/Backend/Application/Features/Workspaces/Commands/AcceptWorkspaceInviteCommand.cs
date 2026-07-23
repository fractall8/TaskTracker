using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record AcceptWorkspaceInviteCommand(string Token) : IRequest<Unit>;

public class AcceptWorkspaceInviteCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceInviteRepository workspaceInviteRepository,
    IRepository<WorkspaceMember, Guid> workspaceMemberRepository,
    IWorkspaceLimitService workspaceLimitService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptWorkspaceInviteCommand, Unit>
{
    public async Task<Unit> Handle(AcceptWorkspaceInviteCommand request, CancellationToken ct)
    {
        var invite = await workspaceInviteRepository.GetByTokenAsync(request.Token, ct);

        if (invite == null || invite.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationException([new ValidationFailure("Token", "Invite link is invalid or has expired.")]);
        }

        var userInfo = await workspaceAccessService.GetCurrentUserInfoAsync(ct);

        var existingRole = await workspaceRepository.GetUserRoleAsync(invite.WorkspaceId, userInfo.UserId, ct);

        if (existingRole != null)
        {
            throw new ValidationException([new ValidationFailure("Token", "You are already a member of this workspace.")]);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.AcquireDistributedLockAsync($"workspace:{invite.WorkspaceId}:members", token);

            await workspaceLimitService.EnsureCanAddWorkspaceMemberAsync(invite.WorkspaceId, token);

            var member = new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = invite.WorkspaceId,
                UserId = userInfo.UserId,
                Role = WorkspaceRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            };

            await workspaceMemberRepository.AddAsync(member, token);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        return Unit.Value;
    }
}
