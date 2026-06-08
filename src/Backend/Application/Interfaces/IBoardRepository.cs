using Domain.Entities;

namespace Application.Interfaces;

public interface IBoardRepository : IRepository<Board, Guid>
{
    Task<Board?> GetBoardWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<bool> IsUserAdminAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}