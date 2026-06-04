using Domain.Entities;

namespace Application.Interfaces;

public interface IBoardRepository : IRepository<Board, Guid>
{
    Task<Board?> GetBoardWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default);
}