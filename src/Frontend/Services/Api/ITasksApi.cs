using Contracts.DTOs;
using Contracts.Requests;
using Refit;

namespace Services.Api;

public interface ITasksApi
{
    [Get("/api/boards/{boardId}/tasks")]
    Task<IApiResponse<List<TaskDto>>> GetAllForBoardAsync(Guid boardId, CancellationToken ct = default);

    [Get("/api/boards/{boardId}/tasks/{taskId}")]
    Task<IApiResponse<TaskDto>> GetByIdAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Multipart]
    [Post("/api/boards/{boardId}/tasks/{taskId}/attachments")]
    Task<IApiResponse<AttachmentDto>> UploadAttachmentAsync(
        Guid boardId, 
        Guid taskId, 
        [AliasAs("file")] StreamPart file, 
        CancellationToken ct = default);
    
    [Post("/api/boards/{boardId}/tasks/columns/{columnId}")]
    Task<IApiResponse<TaskDto>> CreateAsync(Guid boardId, Guid columnId, [Body] CreateTaskRequest request,
        CancellationToken ct = default);

    [Put("/api/boards/{boardId}/tasks/{taskId}")]
    Task<IApiResponse<TaskDto>> UpdateAsync(Guid boardId, Guid taskId, [Body] UpdateTaskRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/tasks/{taskId}")]
    Task<IApiResponse> DeleteAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/move")]
    Task<IApiResponse> MoveAsync(Guid boardId, Guid taskId, [Body] MoveTaskRequest request,
        CancellationToken ct = default);
    
    [Get("/api/boards/{boardId}/tasks/{taskId}/comments")]
    Task<IApiResponse<List<CommentDto>>> GetCommentsAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/comments")]
    Task<IApiResponse<CommentDto>> CreateCommentAsync(Guid boardId, Guid taskId, [Body] CreateCommentRequest request, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/tasks/{taskId}/comments/{commentId}")]
    Task<IApiResponse> DeleteCommentAsync(Guid boardId, Guid taskId, Guid commentId, CancellationToken ct = default);
}