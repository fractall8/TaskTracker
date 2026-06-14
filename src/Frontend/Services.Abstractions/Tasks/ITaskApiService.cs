using Contracts.DTOs;
using Contracts.Requests;

namespace Services.Abstractions.Tasks;

public interface ITaskApiService
{
    Task<List<TaskDto>> GetTasksForBoardAsync(Guid boardId, CancellationToken ct = default);
    
    Task<TaskDto> CreateTaskAsync(Guid boardId, Guid columnId, CreateTaskRequest request, CancellationToken ct = default);
    
    Task<TaskDto> UpdateTaskAsync(Guid boardId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);
    
    Task DeleteTaskAsync(Guid boardId, Guid taskId, CancellationToken ct = default);
    
    Task MoveTaskAsync(Guid boardId, Guid taskId, MoveTaskRequest request, CancellationToken ct = default);
}