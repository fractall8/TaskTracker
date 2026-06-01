using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;


namespace Persistence.Repositories;

public class ColumnRepository(TaskTrackerDbContext dbContext) : Repository<Column, Guid>(dbContext), IColumnRepository
{
    public async Task UpdatePositionsAsync(Dictionary<Guid, int> columnPositions, CancellationToken cancellationToken)
    {
        var columnIds = columnPositions.Keys;
        
        var columns = await _dbSet.Where(c => columnIds.Contains(c.Id)).ToListAsync(cancellationToken);

        foreach (var column in columns)
        {
            if (columnPositions.TryGetValue(column.Id, out var newPosition))
            {
                column.Position = newPosition;
            }
        }
    }
}