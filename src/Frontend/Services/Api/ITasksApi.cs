using Contracts.DTOs;
using Contracts.Requests.Comments;
using Contracts.Requests.Tasks;
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

    [Get("/api/boards/{boardId}/tasks/{taskId}/attachments/{attachmentId}/download")]
    Task<IApiResponse<AttachmentDownloadDto>> GetAttachmentDownloadUrlAsync(Guid boardId, Guid taskId, Guid attachmentId, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/tasks/{taskId}/attachments/{attachmentId}")]
    Task<IApiResponse> DeleteAttachmentAsync(Guid boardId, Guid taskId, Guid attachmentId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/columns/{columnId}")]
    Task<IApiResponse<TaskDto>> CreateAsync(Guid boardId, Guid columnId, [Body] CreateTaskRequest request,
        CancellationToken ct = default);

    [Put("/api/boards/{boardId}/tasks/{taskId}")]
    Task<IApiResponse<TaskDto>> UpdateAsync(Guid boardId, Guid taskId, [Body] UpdateTaskRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/tasks/{taskId}/due-date")]
    Task<IApiResponse<TaskDto>> UpdateDueDateAsync(Guid boardId, Guid taskId, [Body] UpdateTaskDueDateRequest request, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/tags/{tagId}")]
    Task<IApiResponse<TaskDto>> AttachTagAsync(Guid boardId, Guid taskId, Guid tagId, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/tasks/{taskId}/tags/{tagId}")]
    Task<IApiResponse<TaskDto>> DetachTagAsync(Guid boardId, Guid taskId, Guid tagId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/complete")]
    Task<IApiResponse<TaskDto>> CompleteAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/reopen")]
    Task<IApiResponse<TaskDto>> ReopenAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/tasks/{taskId}")]
    Task<IApiResponse> DeleteAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/move")]
    Task<IApiResponse> MoveAsync(Guid boardId, Guid taskId, [Body] MoveTaskRequest request,
        CancellationToken ct = default);

    [Get("/api/boards/{boardId}/tasks/{taskId}/comments")]
    Task<IApiResponse<List<CommentDto>>> GetCommentsAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/tasks/{taskId}/comments")]
    Task<IApiResponse<CommentDto>> CreateCommentAsync(Guid boardId, Guid taskId, [Body] CreateCommentRequest request, CancellationToken ct = default);

    [Put("/api/boards/{boardId}/tasks/{taskId}/comments/{commentId}")]
    Task<ApiResponse<CommentDto>> UpdateCommentAsync(
        Guid boardId,
        Guid taskId,
        Guid commentId,
        [Body] UpdateCommentRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/tasks/{taskId}/comments/{commentId}")]
    Task<IApiResponse> DeleteCommentAsync(Guid boardId, Guid taskId, Guid commentId, CancellationToken ct = default);
}
