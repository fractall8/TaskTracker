using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BoardRepository(TaskTrackerDbContext dbContext) : Repository<Board, Guid>(dbContext), IBoardRepository
{
    public async Task<Board?> GetBoardWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.Id.Equals(id), cancellationToken);
    }
}