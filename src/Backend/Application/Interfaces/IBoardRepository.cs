using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IBoardRepository : IRepository<Board, Guid>
{
    Task<Board?> GetBoardWithHierarchyAsync(Guid boardId, string? searchTerm = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default);

    Task<int> CountUserBoardsAsync(Guid userId, string? searchTerm = null, CancellationToken ct = default);

    Task<List<Board>> GetUserBoardsPaginatedAsync(Guid userId, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}
