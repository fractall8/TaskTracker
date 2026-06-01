using Domain.Entities;

namespace Application.Interfaces;

public interface IColumnRepository : IRepository<Column, Guid>
{
    Task UpdatePositionsAsync(Dictionary<Guid, int> columnPositions, CancellationToken cancellationToken = default);
}