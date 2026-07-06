using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BoardMemberRepository(TaskTrackerDbContext dbContext)
    : Repository<BoardMember, Guid>(dbContext), IBoardMemberRepository
{
    public async Task<List<BoardMember>> GetByWorkspaceMemberIdAsync(Guid workspaceMemberId, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(bm => bm.WorkspaceMemberId == workspaceMemberId).ToListAsync(cancellationToken);
    }

    public async Task<BoardMember?> GetByBoardAndUserIdAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(bm => bm.WorkspaceMember)
            .FirstOrDefaultAsync(bm => bm.BoardId == boardId && bm.WorkspaceMember!.UserId == userId, ct);
    }


    public async Task<bool> RemoveUserFromBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var softDeletedRows = await DbSet
            .Where(bm => bm.BoardId == boardId && bm.WorkspaceMember!.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.IsDeleted, true)
                .SetProperty(b => b.DeletedAt, DateTimeOffset.UtcNow), ct);

        return softDeletedRows > 0;
    }

    public async Task AddUserToAllWorkspaceBoardsAsAdminAsync(Guid workspaceId, Guid workspaceMemberId, CancellationToken ct = default)
    {
        var boardIds = await DbContext.Boards
            .Where(b => b.WorkspaceId == workspaceId && !b.IsDeleted)
            .Select(b => b.Id)
            .ToListAsync(ct);

        if (!boardIds.Any())
        {
            return;
        }

        var existingMemberships = await DbContext.BoardMembers
            .Where(bm => bm.WorkspaceMemberId == workspaceMemberId && boardIds.Contains(bm.BoardId) && !bm.IsDeleted)
            .ToListAsync(ct);

        var existingBoardIds = existingMemberships.Select(m => m.BoardId).ToHashSet();

        foreach (var membership in existingMemberships)
        {
            membership.Role = BoardRole.Admin;
        }

        var missingBoardIds = boardIds.Except(existingBoardIds);

        var newMemberships = missingBoardIds.Select(boardId => new BoardMember
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            WorkspaceMemberId = workspaceMemberId,
            Role = BoardRole.Admin,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await DbContext.BoardMembers.AddRangeAsync(newMemberships, ct);
    }

    public async Task DowngradeUserOnAllWorkspaceBoardsToUserAsync(Guid workspaceId, Guid workspaceMemberId, CancellationToken ct = default)
    {
        var memberships = await DbContext.BoardMembers
            .Where(bm => bm.WorkspaceMemberId == workspaceMemberId
                         && bm.Board!.WorkspaceId == workspaceId
                         && !bm.IsDeleted)
            .ToListAsync(ct);

        foreach (var membership in memberships)
        {
            membership.Role = BoardRole.User;
        }

        DbContext.BoardMembers.UpdateRange(memberships);
    }
}
