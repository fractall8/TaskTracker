using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record AcceptWorkspaceInviteCommand(string Token) : IRequest<Unit>;

public class AcceptWorkspaceInviteCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceInviteRepository workspaceInviteRepository,
    IRepository<WorkspaceMember, Guid> workspaceMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptWorkspaceInviteCommand, Unit>
{
    public async Task<Unit> Handle(AcceptWorkspaceInviteCommand request, CancellationToken ct)
    {
        WorkspaceInvite? invite = await workspaceInviteRepository.GetByTokenAsync(request.Token, ct);

        if (invite == null || invite.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Invite token is invalid or has expired.");

        Guid userId = await workspaceAccessService.GetCurrentUserIdAsync(ct);

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = invite.WorkspaceId,
            UserId = userId,
            Role = WorkspaceRole.Member,
            JoinedAt = DateTimeOffset.UtcNow
        };

        await workspaceMemberRepository.AddAsync(member, ct);

        workspaceInviteRepository.Delete(invite);

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
