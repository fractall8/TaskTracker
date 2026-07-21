using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Requests.Tasks;

namespace Services.Abstractions.Boards;

public interface IBoardDetailsStore
{
    Guid? BoardId { get; }

    BoardWithColumnsDto? Board { get; }

    List<TaskDto> Tasks { get; }

    bool IsLoading { get; }

    string? ErrorMessage { get; }

    event Action? StateChanged;

    event Action? RemoteBoardNameApplied;

    void Reset();

    Task LoadAsync(Guid boardId, string? searchTerm = null, CancellationToken ct = default);

    Task<ColumnDto> CreateColumnAsync(string name, CancellationToken ct = default);

    Task UpdateColumnNameAsync(Guid columnId, string name, CancellationToken ct = default);

    Task DeleteColumnAsync(Guid columnId, CancellationToken ct = default);

    Task ReorderColumnsAsync(Guid columnId, int newPosition, CancellationToken ct = default);

    Task<AttachmentDto> UploadTaskAttachmentAsync(Guid taskId, Refit.StreamPart filePart, CancellationToken ct = default);

    Task<AttachmentDownloadDto> GetTaskAttachmentDownloadUrlAsync(Guid taskId, Guid attachmentId, CancellationToken ct = default);

    Task DeleteTaskAttachmentAsync(Guid taskId, Guid attachmentId, CancellationToken ct = default);

    Task UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);

    Task<TaskDto?> UpdateTaskDueDateAsync(Guid taskId, UpdateTaskDueDateRequest request, CancellationToken ct = default);

    Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default);

    Task CreateTaskAsync(Guid columnId, CreateTaskRequest request, CancellationToken ct = default);

    Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int newPosition, CancellationToken ct = default);

    void SetBoardArchived(BoardExportOptionsDto exportOptions);

    void SetBoardReExporting(BoardExportOptionsDto options);

    void ApplyAction(BoardActionNotification notification, Guid currentUserId);
}
