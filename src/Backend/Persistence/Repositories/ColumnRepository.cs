using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class ColumnRepository(TaskTrackerDbContext dbContext)
    : Repository<Column, Guid>(dbContext), IColumnRepository
{
    public async Task<IEnumerable<string>> GetNameListByBoardIdAsync(Guid boardId, CancellationToken ct = default)
    {
        return await _dbContext.Columns.Where(c => c.BoardId == boardId && !c.IsDeleted).Select(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task DecrementPositionsAsync(Guid boardId, int startingFromPosition, CancellationToken ct)
    {
        await _dbContext.Columns
            .Where(c => c.BoardId == boardId && c.Position > startingFromPosition)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1), ct);
    }
}