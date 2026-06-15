using Application.Interfaces;
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
}