using Domain.Entities;

namespace Application.Interfaces;

public interface ITaskRepository : IRepository<TaskItem, Guid>
{
    Task<IReadOnlyList<TaskItem>> SearchTasksAsync(
        string? searchTerm,
        DateTimeOffset? deadlineBefore, 
        CancellationToken cancellationToken = default);
}