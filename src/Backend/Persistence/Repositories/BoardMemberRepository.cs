using Application.Interfaces.Repositories;
using Domain.Entities;
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
}
