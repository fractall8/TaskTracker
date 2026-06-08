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

    public async Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Boards
            .AsNoTracking()
            .Include(b => b.Members.Where(m => m.UserId == userId))
            .Where(b => b.Members.Any(m => m.UserId == userId))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Set<BoardMember>()
            .Where(m => m.BoardId == boardId && m.UserId == userId)
            .Select(m => (BoardRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }
}