using Domain.Entities;

namespace Application.Interfaces;

public interface ITaskRepository : IRepository<TaskItem, Guid>
{
    Task LoadUsersForTaskAsync(TaskItem task, CancellationToken ct = default);
    
    Task<TaskItem?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken ct = default);

    Task<IEnumerable<TaskItem>> GetTasksByBoardIdAsync(Guid boardId, CancellationToken ct = default);
    
    Task<TaskItem?> GetTaskWithColumnAsync(Guid taskId, CancellationToken ct = default);
    
    Task<int> GetMaxPositionAsync(Guid columnId, CancellationToken ct = default);
    
    Task DecrementPositionsAsync(Guid columnId, int startingFromPosition, CancellationToken ct = default);
    
    Task IncrementPositionsAsync(Guid columnId, int startingFromPosition, CancellationToken ct = default);

    Task UpdatePositionsOnMoveAsync(Guid columnId, int oldPosition, int newPosition, CancellationToken ct = default);
    
    Task<IEnumerable<Attachment>> GetAttachmentsByColumnIdAsync(Guid columnId, CancellationToken ct = default);
    
    Task SoftDeleteTasksAndRelationsByColumnIdAsync(Guid columnId, CancellationToken ct = default);
}