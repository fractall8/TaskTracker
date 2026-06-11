using Domain.Entities;

namespace Application.Interfaces;

public interface IColumnRepository : IRepository<Column, Guid>
{
    Task<IEnumerable<string>> GetNameListByBoardIdAsync(Guid boardId, CancellationToken ct = default);
    
    Task DecrementPositionsAsync(Guid boardId, int startingFromPosition, CancellationToken ct);
}