using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class CommentRepository(TaskTrackerDbContext dbContext)
    : Repository<Comment, Guid>(dbContext), ICommentRepository
{
    public async Task<List<Comment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await DbContext.Set<Comment>()
            .Where(c => c.TaskId == taskId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Comment?> GetCommentWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await DbContext.Set<Comment>()
            .Include(c => c.Task)
            .ThenInclude(t => t.Column)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
