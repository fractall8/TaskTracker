using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IColumnRepository : IRepository<Column, Guid>
{
    Task<IEnumerable<string>> GetNameListByBoardIdAsync(Guid boardId, CancellationToken ct = default);

    Task DecrementPositionsAsync(Guid boardId, int startingFromPosition, CancellationToken ct);

    Task UpdatePositionsOnMoveAsync(Guid boardId, int oldPosition, int newPosition, CancellationToken ct);

    Task<int> GetMaxPositionAsync(Guid boardId, CancellationToken ct = default);

    Task SoftDeleteCascadeAsync(Guid columnId, CancellationToken ct = default);
}
