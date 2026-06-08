using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BoardRepository(TaskTrackerDbContext dbContext) : Repository<Board, Guid>(dbContext), IBoardRepository
{
    public async Task<Board?> GetBoardWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.Id.Equals(id), cancellationToken);
    }

    // Will be used to check whether user have at least one role of allowedRoles
    public async Task<bool> HasRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default, params BoardRole[] allowedRoles)
    {
        if (allowedRoles == null || allowedRoles.Length == 0)
        {
            return false;
        }

        return await _dbContext.Set<BoardMember>()
            .AnyAsync(m => 
                    m.BoardId == boardId && 
                    m.UserId == userId && 
                    allowedRoles.Contains(m.Role),
                ct);
    }
}