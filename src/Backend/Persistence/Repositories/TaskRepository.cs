using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class TaskRepository(TaskTrackerDbContext dbContext) : Repository<TaskItem, Guid>(dbContext), ITaskRepository
{
    public async Task<TaskItem?> GetTaskWithColumnAsync(Guid taskId, CancellationToken ct)
    {
        return await DbContext.Tasks
            .Include(t => t.Column)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);
    }
    
    public async Task<int> GetMaxPositionAsync(Guid columnId, CancellationToken ct = default)
    {
        return await DbContext.Tasks
            .Where(t => t.ColumnId == columnId)
            .MaxAsync(t => (int?)t.Position, ct) ?? -1;
    }

    public async Task DecrementPositionsAsync(Guid columnId, int startingFromPosition, CancellationToken ct = default)
    {
        await DbContext.Tasks
            .Where(t => t.ColumnId == columnId && t.Position >= startingFromPosition)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Position, t => t.Position - 1), ct);
    }

    public async Task IncrementPositionsAsync(Guid columnId, int startingFromPosition, CancellationToken ct = default)
    {
        await DbContext.Tasks
            .Where(t => t.ColumnId == columnId && t.Position >= startingFromPosition)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Position, t => t.Position + 1), ct);
    }

    public async Task UpdatePositionsOnMoveAsync(Guid columnId, int oldPosition, int newPosition, CancellationToken ct = default)
    {
        if (oldPosition < newPosition)
        {
            await DbContext.Tasks
                .Where(t => t.ColumnId == columnId && t.Position > oldPosition && t.Position <= newPosition)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Position, t => t.Position - 1), ct);
        }
        else if (oldPosition > newPosition)
        {
            await DbContext.Tasks
                .Where(t => t.ColumnId == columnId && t.Position >= newPosition && t.Position < oldPosition)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Position, t => t.Position + 1), ct);
        }
    }
}