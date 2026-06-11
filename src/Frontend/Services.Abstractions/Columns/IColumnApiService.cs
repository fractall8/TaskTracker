using Contracts.DTOs;

namespace Services.Abstractions.Columns;

public interface IColumnApiService
{
    Task<ColumnDto> CreateColumnAsync(Guid boardId, string name, CancellationToken ct = default);
    
    Task<ColumnDto> UpdateColumnAsync(Guid boardId, Guid columnId, string name, CancellationToken ct = default);
    
    Task DeleteColumnAsync(Guid boardId, Guid columnId, CancellationToken ct = default);
    
    Task MoveColumnAsync(Guid boardId, Guid columnId, int newPosition, CancellationToken ct = default);
}