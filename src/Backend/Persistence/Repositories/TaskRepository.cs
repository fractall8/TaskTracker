using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class TaskRepository(TaskTrackerDbContext dbContext) : Repository<TaskItem, Guid>(dbContext), ITaskRepository
{
    public async Task LoadUsersForTaskAsync(TaskItem task, CancellationToken ct = default)
    {
        await DbContext.Entry(task).Reference(t => t.Reporter).LoadAsync(ct);

        if (task.AssigneeId.HasValue)
        {
            await DbContext.Entry(task).Reference(t => t.Assignee).LoadAsync(ct);
        }
    }

    public async Task<TaskItem?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken ct = default)
    {
        return await DbContext.Tasks
            .Include(t => t.Column)
            .Include(t => t.Attachments)
            .Include(t => t.Reporter)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByBoardIdAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Tasks
            .Include(t => t.Column)
            .Include(t => t.Reporter)
            .Include(t => t.Assignee)
            .Where(t => t.Column!.BoardId == boardId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);
    }

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
            .MaxAsync(t => (int?)t.Position, ct) ?? 0;
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

    public async Task UpdatePositionsOnMoveAsync(Guid columnId, int oldPosition, int newPosition,
        CancellationToken ct = default)
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

    public async Task<IEnumerable<Attachment>> GetAttachmentsByColumnIdAsync(Guid columnId,
        CancellationToken ct = default)
    {
        return await DbContext.Tasks
            .Where(t => t.ColumnId == columnId)
            .SelectMany(t => t.Attachments)
            .ToListAsync(ct);
    }

    public async Task SoftDeleteTasksAndRelationsByColumnIdAsync(Guid columnId, CancellationToken ct = default)
    {
        var deletedAt = DateTime.UtcNow;

        await DbContext.Set<Attachment>()
            .IgnoreQueryFilters()
            .Where(a => a.Task!.ColumnId == columnId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.DeletedAt, deletedAt), ct);

        await DbContext.Set<Comment>()
            .IgnoreQueryFilters()
            .Where(c => c.Task!.ColumnId == columnId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.DeletedAt, deletedAt), ct);

        await DbContext.Set<TaskItem>()
            .IgnoreQueryFilters()
            .Where(t => t.ColumnId == columnId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsDeleted, true)
                .SetProperty(t => t.DeletedAt, deletedAt), ct);
    }
}
