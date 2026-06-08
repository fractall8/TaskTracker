using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IBoardRepository : IRepository<Board, Guid>
{
    Task<Board?> GetBoardWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default);
    
    Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}