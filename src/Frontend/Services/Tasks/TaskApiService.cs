using Contracts.DTOs;
using Contracts.Requests.Comments;
using Contracts.Requests.Tasks;
using Refit;
using Services.Abstractions.Tasks;
using Services.Api;
using Services.Extensions;

namespace Services.Tasks;

public class TaskApiService(ITasksApi tasksApi) : ITaskApiService
{
    public async Task<List<TaskDto>> GetTasksForBoardAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await tasksApi.GetAllForBoardAsync(boardId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<TaskDto> GetTaskByIdAsync(Guid boardId, Guid taskId, CancellationToken ct = default)
    {
        var response = await tasksApi.GetByIdAsync(boardId, taskId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<TaskDto> CreateTaskAsync(Guid boardId, Guid columnId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var response = await tasksApi.CreateAsync(boardId, columnId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid boardId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var response = await tasksApi.UpdateAsync(boardId, taskId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task DeleteTaskAsync(Guid boardId, Guid taskId, CancellationToken ct = default)
    {
        var response = await tasksApi.DeleteAsync(boardId, taskId, ct);
        await response.HandleResponseAsync();
    }

    public async Task MoveTaskAsync(Guid boardId, Guid taskId, MoveTaskRequest request, CancellationToken ct = default)
    {
        var response = await tasksApi.MoveAsync(boardId, taskId, request, ct);
        await response.HandleResponseAsync();
    }

    public async Task<AttachmentDto> UploadAttachmentAsync(Guid boardId, Guid taskId, StreamPart filePart, CancellationToken ct = default)
    {
        var response = await tasksApi.UploadAttachmentAsync(boardId, taskId, filePart, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<List<CommentDto>> GetCommentsAsync(Guid boardId, Guid taskId, CancellationToken ct = default)
    {
        var response = await tasksApi.GetCommentsAsync(boardId, taskId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<CommentDto> CreateCommentAsync(Guid boardId, Guid taskId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var response = await tasksApi.CreateCommentAsync(boardId, taskId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid boardId, Guid taskId, Guid commentId, UpdateCommentRequest request,
        CancellationToken ct = default)
    {
        var response = await tasksApi.UpdateCommentAsync(boardId, taskId, commentId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task DeleteCommentAsync(Guid boardId, Guid taskId, Guid commentId, CancellationToken ct = default)
    {
        var response = await tasksApi.DeleteCommentAsync(boardId, taskId, commentId, ct);
        await response.HandleResponseAsync();
    }
}
