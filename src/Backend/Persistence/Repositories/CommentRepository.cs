using Application.Interfaces;
using Domain.Entities;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class CommentRepository(TaskTrackerDbContext dbContext)
    : Repository<Comment, Guid>(dbContext), ICommentRepository
{
    public Task<List<Comment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}