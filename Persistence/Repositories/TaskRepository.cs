using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class TaskRepository(TaskTrackerDbContext dbContext) : Repository<TaskItem, Guid>(dbContext), ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> SearchTasksAsync(string? searchTerm, DateTimeOffset? deadlineBefore,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                t.Title.Contains(searchTerm) || 
                (t.Description ?? string.Empty).Contains(searchTerm));
        }

        if (deadlineBefore.HasValue)
        {
            query = query.Where(t => t.DueDate <= deadlineBefore.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}