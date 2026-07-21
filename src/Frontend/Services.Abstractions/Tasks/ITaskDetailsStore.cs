using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Requests.Comments;
using Contracts.Requests.Tasks;
using Refit;

namespace Services.Abstractions.Tasks;

public interface ITaskDetailsStore
{
    Guid? BoardId { get; }
    TaskDto? Task { get; }
    List<CommentDto>? Comments { get; }
    bool IsLoading { get; }
    string? ErrorMessage { get; }

    event Action? StateChanged;

    void Reset();
    Task LoadAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    Task UpdateTaskAsync(Guid boardId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);

    Task UpdateTaskDueDateAsync(Guid boardId, Guid taskId, UpdateTaskDueDateRequest request,
        CancellationToken ct = default);

    Task DeleteTaskAsync(Guid boardId, Guid taskId, CancellationToken ct = default);

    Task<AttachmentDto> UploadAttachmentAsync(Guid boardId, Guid taskId, StreamPart filePart,
        CancellationToken ct = default);

    Task<AttachmentDownloadDto> GetAttachmentDownloadUrlAsync(Guid boardId, Guid taskId, Guid attachmentId,
        CancellationToken ct = default);

    Task DeleteAttachmentAsync(Guid boardId, Guid taskId, Guid attachmentId, CancellationToken ct = default);

    Task CreateCommentAsync(Guid boardId, Guid taskId, CreateCommentRequest request, CancellationToken ct = default);

    Task UpdateCommentAsync(Guid boardId, Guid taskId, Guid commentId, UpdateCommentRequest request,
        CancellationToken ct = default);

    Task DeleteCommentAsync(Guid boardId, Guid taskId, Guid commentId, CancellationToken ct = default);

    void ApplyAction(BoardActionNotification notification, Guid currentUserId);
}
