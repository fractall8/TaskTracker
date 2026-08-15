using Contracts.DTOs;
using Contracts.Requests.Comments;
using Contracts.Requests.Tasks;
using Refit;

namespace Services.Abstractions.Tasks;

public interface ITaskApiService
{
    Task<List<TaskDto>> GetTasksForBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<TaskDto> GetTaskByIdAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    Task<AttachmentDto> UploadAttachmentAsync(Guid boardId, Guid taskId, StreamPart filePart, CancellationToken ct = default);

    Task<AttachmentDownloadDto> GetAttachmentDownloadUrlAsync(Guid boardId, Guid taskId, Guid attachmentId, CancellationToken ct = default);

    Task DeleteAttachmentAsync(Guid boardId, Guid taskId, Guid attachmentId, CancellationToken ct = default);

    Task<TaskDto> CreateTaskAsync(Guid boardId, Guid columnId, CreateTaskRequest request, CancellationToken ct = default);

    Task<TaskDto> UpdateTaskAsync(Guid boardId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);

    Task<TaskDto> UpdateTaskDueDateAsync(Guid boardId, Guid taskId, UpdateTaskDueDateRequest request,
        CancellationToken ct = default);

    Task<TaskDto> SetTaskCompletionAsync(Guid boardId, Guid taskId, bool isCompleted, CancellationToken ct = default);

    Task<TaskDto> AttachTagAsync(Guid boardId, Guid taskId, Guid tagId, CancellationToken ct = default);

    Task<TaskDto> DetachTagAsync(Guid boardId, Guid taskId, Guid tagId, CancellationToken ct = default);

    Task DeleteTaskAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    Task MoveTaskAsync(Guid boardId, Guid taskId, MoveTaskRequest request, CancellationToken ct = default);

    Task<List<CommentDto>> GetCommentsAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    Task<CommentDto> CreateCommentAsync(Guid boardId, Guid taskId, CreateCommentRequest request,
        CancellationToken ct = default);

    Task<CommentDto> UpdateCommentAsync(Guid boardId, Guid taskId, Guid commentId, UpdateCommentRequest request,
        CancellationToken ct = default);

    Task DeleteCommentAsync(Guid boardId, Guid taskId, Guid commentId, CancellationToken ct = default);
}
