using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Contracts.Requests.Comments;
using Contracts.Requests.Tasks;
using Microsoft.Extensions.Logging;
using Refit;
using Services.Abstractions.Boards;
using Services.Abstractions.Tasks;

namespace Services.Tasks.Stores;

public class TaskDetailsStore(
    ITaskApiService taskApi,
    IBoardActionSyncGuard syncGuard,
    ILogger<TaskDetailsStore> logger) : ITaskDetailsStore
{
    public Guid? BoardId { get; private set; }
    public TaskDto? Task { get; private set; }
    public List<CommentDto>? Comments { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;
    private void NotifyStateChanged() => StateChanged?.Invoke();

    public void Reset()
    {
        BoardId = null;
        Task = null;
        Comments = null;
        IsLoading = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    public async Task LoadAsync(Guid boardId, Guid taskId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        BoardId = boardId;
        NotifyStateChanged();

        try
        {
            var taskTask = taskApi.GetTaskByIdAsync(boardId, taskId, ct);
            var commentsTask = taskApi.GetCommentsAsync(boardId, taskId, ct);

            await System.Threading.Tasks.Task.WhenAll(taskTask, commentsTask);

            Task = taskTask.Result;
            Comments = commentsTask.Result;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            logger.LogError(ex, "Failed to load task details for TaskId: {TaskId}", taskId);
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task UpdateTaskAsync(Guid boardId, Guid taskId, UpdateTaskRequest request,
        CancellationToken ct = default)
    {
        var updatedTask = await taskApi.UpdateTaskAsync(boardId, taskId, request, ct);

        var currentAttachments = Task?.Attachments ?? [];
        Task = updatedTask with { Attachments = currentAttachments };
        NotifyStateChanged();
    }

    public async Task UpdateTaskDueDateAsync(Guid boardId, Guid taskId, UpdateTaskDueDateRequest request,
        CancellationToken ct = default)
    {
        var updatedTask = await taskApi.UpdateTaskDueDateAsync(boardId, taskId, request, ct);

        var currentAttachments = Task?.Attachments ?? [];
        Task = updatedTask with { Attachments = currentAttachments };
        NotifyStateChanged();
    }

    public async Task DeleteTaskAsync(Guid boardId, Guid taskId, CancellationToken ct = default)
    {
        await taskApi.DeleteTaskAsync(boardId, taskId, ct);
        Reset();
    }

    public async Task<AttachmentDto> UploadAttachmentAsync(Guid boardId, Guid taskId, StreamPart filePart,
        CancellationToken ct = default)
    {
        var attachment = await taskApi.UploadAttachmentAsync(boardId, taskId, filePart, ct);

        Task?.Attachments.Add(attachment);
        NotifyStateChanged();

        return attachment;
    }

    public Task<AttachmentDownloadDto> GetAttachmentDownloadUrlAsync(Guid boardId, Guid taskId, Guid attachmentId,
        CancellationToken ct = default)
    {
        return taskApi.GetAttachmentDownloadUrlAsync(boardId, taskId, attachmentId, ct);
    }

    public async Task DeleteAttachmentAsync(Guid boardId, Guid taskId, Guid attachmentId,
        CancellationToken ct = default)
    {
        await taskApi.DeleteAttachmentAsync(boardId, taskId, attachmentId, ct);

        var att = Task?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (att != null)
        {
            Task?.Attachments.Remove(att);
            NotifyStateChanged();
        }
    }

    public async Task CreateCommentAsync(Guid boardId, Guid taskId, CreateCommentRequest request,
        CancellationToken ct = default)
    {
        var newComment = await taskApi.CreateCommentAsync(boardId, taskId, request, ct);
        Comments?.Add(newComment);
        NotifyStateChanged();
    }

    public async Task UpdateCommentAsync(Guid boardId, Guid taskId, Guid commentId, UpdateCommentRequest request,
        CancellationToken ct = default)
    {
        var updatedComment = await taskApi.UpdateCommentAsync(boardId, taskId, commentId, request, ct);

        var index = Comments?.FindIndex(c => c.Id == commentId) ?? -1;
        if (index != -1 && Comments != null)
        {
            Comments[index] = updatedComment;
            NotifyStateChanged();
        }
    }

    public async Task DeleteCommentAsync(Guid boardId, Guid taskId, Guid commentId, CancellationToken ct = default)
    {
        await taskApi.DeleteCommentAsync(boardId, taskId, commentId, ct);
        Comments?.RemoveAll(c => c.Id == commentId);
        NotifyStateChanged();
    }

    public void ApplyAction(BoardActionNotification notification, Guid currentUserId)
    {
        if (Task == null || BoardId == null)
        {
            return;
        }

        if (!syncGuard.TryAccept(notification, BoardId.Value, currentUserId))
        {
            return;
        }

        bool applied = notification.Type switch
        {
            BoardActionNotificationType.TaskUpdated => ApplyTaskUpdated((TaskUpdatedPayload)notification.Payload),
            BoardActionNotificationType.CommentAdded => ApplyCommentAdded((CommentAddedPayload)notification.Payload),
            BoardActionNotificationType.CommentUpdated => ApplyCommentUpdated(
                (CommentUpdatedPayload)notification.Payload),
            BoardActionNotificationType.CommentDeleted => ApplyCommentDeleted(
                (CommentDeletedPayload)notification.Payload),
            BoardActionNotificationType.AttachmentAdded => ApplyAttachmentAdded(
                (AttachmentAddedPayload)notification.Payload),
            BoardActionNotificationType.AttachmentDeleted => ApplyAttachmentDeleted(
                (AttachmentDeletedPayload)notification.Payload),
            BoardActionNotificationType.TaskDetailsUpdated => ApplyTaskDetailsUpdated((TaskDetailsUpdatedPayload)notification.Payload),
            BoardActionNotificationType.TaskDueDateUpdated => ApplyTaskDueDateUpdated((TaskDueDateUpdatedPayload)notification.Payload),
            _ => false
        };

        if (applied)
        {
            syncGuard.MarkApplied(notification);
            NotifyStateChanged();
        }
    }

    private bool ApplyTaskUpdated(TaskUpdatedPayload payload)
    {
        if (Task == null || Task.Id != payload.BoardTaskId)
        {
            return false;
        }

        Task = Task with
        {
            Title = payload.Title,
            AssigneeId = payload.AssigneeId
        };
        return true;
    }

    private bool ApplyCommentAdded(CommentAddedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        Comments?.Add(payload.Comment);
        return true;
    }

    private bool ApplyCommentUpdated(CommentUpdatedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        var index = Comments?.FindIndex(c => c.Id == payload.CommentId) ?? -1;
        if (index == -1 || Comments == null)
        {
            return false;
        }

        Comments[index] = Comments[index] with { Text = payload.NewText };
        return true;
    }

    private bool ApplyCommentDeleted(CommentDeletedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        return Comments?.RemoveAll(c => c.Id == payload.CommentId) > 0;
    }

    private bool ApplyAttachmentAdded(AttachmentAddedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        Task.Attachments.Add(payload.Attachment);
        return true;
    }

    private bool ApplyAttachmentDeleted(AttachmentDeletedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        var att = Task.Attachments.FirstOrDefault(a => a.Id == payload.AttachmentId);
        if (att == null)
        {
            return false;
        }

        Task.Attachments.Remove(att);
        return true;
    }

    private bool ApplyTaskDetailsUpdated(TaskDetailsUpdatedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        Task = Task with
        {
            Title = payload.Title,
            Description = payload.Description,
            AssigneeId = payload.AssigneeId,
            AssigneeName = payload.AssigneeName,
            AssigneeAvatarUrl = payload.AssigneeAvatarUrl
        };
        return true;
    }

    private bool ApplyTaskDueDateUpdated(TaskDueDateUpdatedPayload payload)
    {
        if (Task == null || Task.Id != payload.TaskId)
        {
            return false;
        }

        Task = Task with
        {
            DueDate = payload.DueDate
        };
        return true;
    }
}
