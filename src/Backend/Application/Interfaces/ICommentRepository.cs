using Domain.Entities;

namespace Application.Interfaces;

public interface ICommentRepository : IRepository<Comment, Guid>
{
    Task<List<Comment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);

    Task<Comment?> GetCommentWithDetailsAsync(Guid id, CancellationToken ct = default);
}
