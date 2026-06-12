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
        return await DbContext.Columns.Where(c => c.BoardId == boardId && !c.IsDeleted).Select(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task DecrementPositionsAsync(Guid boardId, int startingFromPosition, CancellationToken ct)
    {
        await DbContext.Columns
            .Where(c => c.BoardId == boardId && c.Position > startingFromPosition)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1), ct);
    }
    
    
    public async Task UpdatePositionsOnMoveAsync(Guid boardId, int oldPosition, int newPosition, CancellationToken ct)
    {
        if (oldPosition < newPosition)
        {
            await DbContext.Columns
                .Where(c => c.BoardId == boardId && c.Position > oldPosition && c.Position <= newPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1), ct);
        }
        else if (oldPosition > newPosition)
        {
            await DbContext.Columns
                .Where(c => c.BoardId == boardId && c.Position >= newPosition && c.Position < oldPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position + 1), ct);
        }
    }
}