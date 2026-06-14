using Contracts.DTOs;
using Contracts.Requests;
using Services.Abstractions.Tasks;
using Services.Api;

namespace Services.Tasks;

public class TasksApiService(ITasksApi tasksApi) : ITaskApiService
{
    public async Task<List<TaskDto>> GetTasksForBoardAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await tasksApi.GetAllForBoardAsync(boardId, ct);
        if (!response.IsSuccessStatusCode || response.Content == null) throw new Exception("Failed to load tasks");
        return response.Content;
    }

    public async Task<TaskDto> CreateTaskAsync(Guid boardId, Guid columnId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var response = await tasksApi.CreateAsync(boardId, columnId, request, ct);
        if (!response.IsSuccessStatusCode || response.Content == null) throw new Exception("Failed to create task");
        return response.Content;
    }

    public async Task DeleteTaskAsync(Guid boardId, Guid taskId, CancellationToken ct = default)
    {
        var response = await tasksApi.DeleteAsync(boardId, taskId, ct);
        if (!response.IsSuccessStatusCode) throw new Exception("Failed to delete task");
    }

    public async Task MoveTaskAsync(Guid boardId, Guid taskId, MoveTaskRequest request, CancellationToken ct = default)
    {
        var response = await tasksApi.MoveAsync(boardId, taskId, request, ct);
        if (!response.IsSuccessStatusCode) throw new Exception("Failed to move task");
    }
}