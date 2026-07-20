using Contracts.DTOs;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Contracts.Notifications.BoardActions.Payloads.Positions;
using Contracts.Requests.Tasks;
using Microsoft.Extensions.Logging;
using Services.Abstractions.Boards;
using Services.Abstractions.Columns;
using Services.Abstractions.Tasks;

namespace Services.Boards.Stores;

public class BoardDetailsStore(
    IBoardApiService boardsApi,
    IColumnApiService columnsApi,
    ITaskApiService tasksApi,
    IBoardActionSyncGuard syncGuard,
    ILogger<BoardDetailsStore> logger)
    : IBoardDetailsStore
{
    public Guid? BoardId { get; private set; }

    public BoardWithColumnsDto? Board { get; private set; }

    public List<TaskDto> Tasks { get; private set; } = [];

    public bool IsLoading { get; private set; }

    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public event Action? RemoteBoardNameApplied;

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public void Reset()
    {
        BoardId = null;
        Board = null;
        IsLoading = false;
        ErrorMessage = null;
        NotifyStateChanged();
        Tasks.Clear();
    }

    public async Task LoadAsync(Guid boardId, string? searchTerm = null, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var loadedBoard = await boardsApi.GetBoardByIdAsync(boardId, searchTerm, ct);

            BoardId = boardId;
            Board = loadedBoard with
            {
                Columns = loadedBoard.Columns.OrderBy(c => c.Position).ToList()
            };

            if (loadedBoard.Columns != null)
            {
                Tasks = loadedBoard.Columns
                    .SelectMany(c => c.Tasks ?? Enumerable.Empty<TaskDto>())
                    .OrderBy(t => t.ColumnId)
                    .ThenBy(t => t.Position)
                    .ToList();
            }
            else
            {
                Tasks = new List<TaskDto>();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<ColumnDto> CreateColumnAsync(string name, CancellationToken ct = default)
    {
        if (BoardId == null || Board == null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        var newColumn = await columnsApi.CreateColumnAsync(BoardId.Value, name, ct);

        Board = Board with
        {
            Columns = Board.Columns.Append(newColumn).ToList()
        };

        NotifyStateChanged();

        return newColumn;
    }

    public async Task UpdateColumnNameAsync(Guid columnId, string name, CancellationToken ct = default)
    {
        if (BoardId == null || Board == null)
        {
            return;
        }

        var column = Board.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null || column.Name == name)
        {
            return;
        }

        await columnsApi.UpdateColumnAsync(BoardId.Value, columnId, name, ct);

        // Optimistic UI update
        var updatedColumns = Board.Columns.Select(c =>
            c.Id == columnId ? c with { Name = name } : c
        ).ToList();

        Board = Board with { Columns = updatedColumns };
        NotifyStateChanged();
    }

    public async Task DeleteColumnAsync(Guid columnId, CancellationToken ct = default)
    {
        if (BoardId == null || Board == null)
        {
            return;
        }

        var columnToDelete = Board.Columns.FirstOrDefault(c => c.Id == columnId);
        if (columnToDelete == null)
        {
            return;
        }

        await columnsApi.DeleteColumnAsync(BoardId.Value, columnId, ct);

        var updatedColumns = Board.Columns
            .Where(c => c.Id != columnId)
            .Select(c => c.Position > columnToDelete.Position
                ? c with { Position = c.Position - 1 }
                : c)
            .ToList();

        Board = Board with { Columns = updatedColumns };
        NotifyStateChanged();
    }

    public async Task ReorderColumnsAsync(Guid columnId, int newPosition, CancellationToken ct = default)
    {
        if (BoardId == null || Board == null)
        {
            return;
        }

        var columnToMove = Board.Columns.FirstOrDefault(c => c.Id == columnId);
        if (columnToMove == null || columnToMove.Position == newPosition)
        {
            return;
        }

        var oldPosition = columnToMove.Position;

        // Optimistic Update
        var updatedColumns = Board.Columns.Select(c =>
            {
                if (c.Id == columnId)
                {
                    return c with { Position = newPosition };
                }

                if (oldPosition < newPosition && c.Position > oldPosition && c.Position <= newPosition)
                {
                    return c with { Position = c.Position - 1 };
                }

                if (oldPosition > newPosition && c.Position >= newPosition && c.Position < oldPosition)
                {
                    return c with { Position = c.Position + 1 };
                }

                return c;
            })
            .OrderBy(c => c.Position)
            .ToList();

        Board = Board with { Columns = updatedColumns };
        NotifyStateChanged();

        try
        {
            await columnsApi.MoveColumnAsync(BoardId.Value, columnId, newPosition, ct);
        }
        catch
        {
            // If the API fails, reload the board from the server
            await LoadAsync(BoardId.Value, null, ct);
            throw;
        }
    }

    public async Task<AttachmentDto> UploadTaskAttachmentAsync(Guid taskId, Refit.StreamPart filePart,
        CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        var attachment = await tasksApi.UploadAttachmentAsync(BoardId.Value, taskId, filePart, ct);

        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            task.Attachments.Add(attachment);
            NotifyStateChanged();
        }

        return attachment;
    }

    public async Task<AttachmentDownloadDto> GetTaskAttachmentDownloadUrlAsync(Guid taskId, Guid attachmentId,
        CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        return await tasksApi.GetAttachmentDownloadUrlAsync(BoardId.Value, taskId, attachmentId, ct);
    }

    public async Task DeleteTaskAttachmentAsync(Guid taskId, Guid attachmentId, CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            throw new InvalidOperationException("Board is not loaded.");
        }

        await tasksApi.DeleteAttachmentAsync(BoardId.Value, taskId, attachmentId, ct);

        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            var att = task.Attachments.FirstOrDefault(a => a.Id == attachmentId);
            if (att != null)
            {
                task.Attachments.Remove(att);
                NotifyStateChanged();
            }
        }
    }

    public async Task UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            return;
        }

        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
        {
            return;
        }

        var updatedTask = await tasksApi.UpdateTaskAsync(BoardId.Value, taskId, request, ct);

        Tasks = Tasks.Select(t => t.Id == taskId ? updatedTask : t).ToList();
        NotifyStateChanged();
    }

    public async Task<TaskDto?> UpdateTaskDueDateAsync(Guid taskId, UpdateTaskDueDateRequest request,
        CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            return null;
        }

        var updatedTask = await tasksApi.UpdateTaskDueDateAsync(BoardId.Value, taskId, request, ct);

        Tasks = Tasks.Select(t => t.Id == taskId ? updatedTask : t).ToList();
        NotifyStateChanged();

        return updatedTask;
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            return;
        }

        var taskToDelete = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (taskToDelete == null)
        {
            return;
        }

        var columnId = taskToDelete.ColumnId;
        var deletedPosition = taskToDelete.Position;

        await tasksApi.DeleteTaskAsync(BoardId.Value, taskId, ct);

        Tasks = Tasks
            .Where(t => t.Id != taskId)
            .Select(t => t.ColumnId == columnId && t.Position > deletedPosition
                ? t with { Position = t.Position - 1 }
                : t)
            .OrderBy(t => t.ColumnId)
            .ThenBy(t => t.Position)
            .ToList();

        NotifyStateChanged();
    }

    public async Task CreateTaskAsync(Guid columnId, CreateTaskRequest request, CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            return;
        }

        var newTask = await tasksApi.CreateTaskAsync(BoardId.Value, columnId, request, ct);

        Tasks.Add(newTask);

        Tasks = Tasks.OrderBy(t => t.ColumnId).ThenBy(t => t.Position).ToList();

        NotifyStateChanged();
    }
    public async Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int dropIndex, CancellationToken ct = default)
{
    if (BoardId == null)
    {
        return;
    }

    var taskToMove = Tasks.FirstOrDefault(t => t.Id == taskId);
    if (taskToMove == null)
    {
        return;
    }

    var oldColumnId = taskToMove.ColumnId;
    var oldPosition = taskToMove.Position;
    var isSameColumn = oldColumnId == targetColumnId;

    var targetColumnTasks = Tasks
        .Where(t => t.ColumnId == targetColumnId)
        .OrderBy(t => t.Position)
        .ToList();

    int safeNewPosition;
    if (targetColumnTasks.Count == 0)
    {
        safeNewPosition = 0;
    }
    else if (isSameColumn)
    {
        var safeDropIndex = Math.Clamp(dropIndex, 0, targetColumnTasks.Count - 1);
        safeNewPosition = targetColumnTasks[safeDropIndex].Position;
    }
    else
    {
        if (dropIndex >= targetColumnTasks.Count)
        {
            safeNewPosition = targetColumnTasks.Last().Position + 1;
        }
        else
        {
            safeNewPosition = targetColumnTasks[Math.Max(0, dropIndex)].Position;
        }
    }

    if (isSameColumn && oldPosition == safeNewPosition)
    {
        return;
    }

    Tasks = Tasks.Select(t =>
        {
            if (t.Id == taskId)
            {
                return t with { ColumnId = targetColumnId, Position = safeNewPosition };
            }

            if (isSameColumn)
            {
                if (t.ColumnId == targetColumnId)
                {
                    if (oldPosition < safeNewPosition && t.Position > oldPosition && t.Position <= safeNewPosition)
                    {
                        return t with { Position = t.Position - 1 };
                    }

                    if (oldPosition > safeNewPosition && t.Position >= safeNewPosition && t.Position < oldPosition)
                    {
                        return t with { Position = t.Position + 1 };
                    }
                }
                return t;
            }

            if (t.ColumnId == oldColumnId && t.Position > oldPosition)
            {
                return t with { Position = t.Position - 1 };
            }

            if (t.ColumnId == targetColumnId && t.Position >= safeNewPosition)
            {
                return t with { Position = t.Position + 1 };
            }

            return t;
        })
        .OrderBy(t => t.ColumnId)
        .ThenBy(t => t.Position)
        .ToList();

    NotifyStateChanged();

    try
    {
        var request = new MoveTaskRequest(targetColumnId, safeNewPosition);
        await tasksApi.MoveTaskAsync(BoardId.Value, taskId, request, ct);
    }
    catch
    {
        await LoadAsync(BoardId.Value, null, ct);
        throw;
    }
}

    public void SetBoardArchived(BoardExportOptionsDto exportOptions)
    {
        if (Board != null)
        {
            Board = Board with
            {
                IsArchived = true,
                ExportStatus = BoardExportStatusDto.Requested,
                ExportOptions = exportOptions
            };
            NotifyStateChanged();
        }
    }

    public void SetBoardReExporting(BoardExportOptionsDto options)
    {
        if (Board != null)
        {
            Board = Board with
            {
                ReExportStatus = BoardExportStatusDto.Pending,
                ReExportOptions = options
            };
            NotifyStateChanged();
        }
    }

    public void ApplyAction(BoardActionNotification notification, Guid currentUserId)
    {
        if (!syncGuard.TryAccept(notification, BoardId, currentUserId))
        {
            return;
        }

        bool applied = notification.Type switch
        {
            BoardActionNotificationType.BoardRenamed => ApplyBoardRenamed((BoardRenamedPayload)notification.Payload),
            BoardActionNotificationType.ColumnCreated => ApplyColumnCreated((ColumnCreatedPayload)notification.Payload),
            BoardActionNotificationType.ColumnRenamed => ApplyColumnRenamed((ColumnRenamedPayload)notification.Payload),
            BoardActionNotificationType.ColumnDeleted => ApplyColumnDeleted((ColumnDeletedPayload)notification.Payload),
            BoardActionNotificationType.ColumnsReordered => ApplyColumnsReordered(
                (ColumnsReorderedPayload)notification.Payload),
            BoardActionNotificationType.TaskCreated => ApplyTaskCreated((TaskCreatedPayload)notification.Payload),
            BoardActionNotificationType.TaskUpdated => ApplyTaskUpdated((TaskUpdatedPayload)notification.Payload),
            BoardActionNotificationType.TaskDeleted => ApplyTaskDeleted((TaskDeletedPayload)notification.Payload),
            BoardActionNotificationType.TasksReordered => ApplyTasksReordered(
                (TasksReorderedPayload)notification.Payload),

            // Implementation for this exists on backend, but I didn't have it in UI, so skip for now
            // BoardActionNotificationType.TaskCommentsCountChanged => ApplyTaskCommentsCountChanged((TaskCommentsCountChangedPayload)notification.Payload),
            // BoardActionNotificationType.TaskAttachmentsCountChanged => ApplyTaskAttachmentsCountChanged((TaskAttachmentsCountChangedPayload)notification.Payload),

            _ => false
        };

        if (applied)
        {
            syncGuard.MarkApplied(notification);
            NotifyStateChanged();
        }
    }

    private bool ApplyBoardRenamed(BoardRenamedPayload payload)
    {
        if (Board is null || string.IsNullOrWhiteSpace(payload.Name))
        {
            return false;
        }

        Board = Board with { Name = payload.Name };
        return true;
    }

    private bool ApplyColumnCreated(ColumnCreatedPayload payload)
    {
        if (Board is null || string.IsNullOrWhiteSpace(payload.Name))
        {
            return false;
        }

        if (Board.Columns.Any(column => column.Id == payload.ColumnId))
        {
            return true;
        }

        var columnDto = new ColumnDto(payload.ColumnId, payload.Name, payload.Position, []);

        Board = Board with
        {
            Columns = Board.Columns
                .Append(columnDto)
                .OrderBy(column => column.Position)
                .ToList(),
        };

        return true;
    }

    private bool ApplyColumnRenamed(ColumnRenamedPayload payload)
    {
        if (Board is null || string.IsNullOrWhiteSpace(payload.Name))
        {
            return false;
        }

        if (Board.Columns.All(column => column.Id != payload.ColumnId))
        {
            return false;
        }

        Board = Board with
        {
            Columns = Board.Columns
                .Select(column => column.Id == payload.ColumnId ? column with { Name = payload.Name } : column)
                .ToList(),
        };

        return true;
    }

    private bool ApplyColumnDeleted(ColumnDeletedPayload payload)
    {
        if (Board is null)
        {
            return false;
        }

        var deletedColumn = Board.Columns.FirstOrDefault(column => column.Id == payload.ColumnId);
        if (deletedColumn is null)
        {
            return false;
        }

        Board = Board with
        {
            Columns = Board.Columns
                .Where(column => column.Id != payload.ColumnId)
                .ToList(),
        };

        Tasks.RemoveAll(t => t.ColumnId == payload.ColumnId);

        ApplyColumnPositions(payload.RemainingColumns);

        return true;
    }

    private bool ApplyColumnsReordered(ColumnsReorderedPayload payload)
    {
        if (Board is null)
        {
            return false;
        }

        ApplyColumnPositions(payload.Columns);
        return true;
    }

    private bool ApplyTaskCreated(TaskCreatedPayload payload)
    {
        if (Board is null || string.IsNullOrWhiteSpace(payload.Title))
        {
            return false;
        }

        if (Tasks.Any(t => t.Id == payload.BoardTaskId))
        {
            return true;
        }

        var newTask = new TaskDto(
            Id: payload.BoardTaskId,
            Title: payload.Title,
            Description: null,
            Position: payload.Position,
            DueDate: null,
            ColumnId: payload.ColumnId,
            AssigneeId: payload.AssigneeId,
            AssigneeName: null,
            AssigneeAvatarUrl: null,
            ReporterId: Guid.Empty,
            ReporterName: null,
            ReporterAvatarUrl: null,
            Attachments: []
        );

        Tasks.Add(newTask);
        Tasks = Tasks.OrderBy(t => t.ColumnId).ThenBy(t => t.Position).ToList();

        return true;
    }

    private bool ApplyTaskUpdated(TaskUpdatedPayload payload)
    {
        if (Board is null || string.IsNullOrWhiteSpace(payload.Title))
        {
            return false;
        }

        var taskIndex = Tasks.FindIndex(t => t.Id == payload.BoardTaskId);
        if (taskIndex == -1)
        {
            return false;
        }

        var task = Tasks[taskIndex];
        Tasks[taskIndex] = task with
        {
            Title = payload.Title,
            AssigneeId = payload.AssigneeId
        };

        return true;
    }

    private bool ApplyTaskDeleted(TaskDeletedPayload payload)
    {
        if (Board is null)
        {
            return false;
        }

        var removed = Tasks.RemoveAll(t => t.Id == payload.BoardTaskId) > 0;
        if (removed && payload.RemainingTasks != null)
        {
            ApplyColumnTaskPositions(payload.ColumnId, payload.RemainingTasks);
        }

        return true;
    }

    private bool ApplyTasksReordered(TasksReorderedPayload payload)
    {
        if (Board is null)
        {
            return false;
        }

        var taskIndex = Tasks.FindIndex(t => t.Id == payload.BoardTaskId);
        if (taskIndex != -1)
        {
            Tasks[taskIndex] = Tasks[taskIndex] with
            {
                ColumnId = payload.TargetColumnId,
                Position = payload.Position
            };
        }

        if (payload.SourceColumnTasks != null)
        {
            ApplyColumnTaskPositions(payload.SourceColumnId, payload.SourceColumnTasks);
        }

        if (payload.SourceColumnId != payload.TargetColumnId && payload.TargetColumnTasks != null)
        {
            ApplyColumnTaskPositions(payload.TargetColumnId, payload.TargetColumnTasks);
        }

        Tasks = Tasks.OrderBy(t => t.ColumnId).ThenBy(t => t.Position).ToList();
        return true;
    }

    private void ApplyColumnPositions(IReadOnlyList<BoardActionColumnPosition> columnPositions)
    {
        if (Board is null || columnPositions == null || columnPositions.Count == 0)
        {
            return;
        }

        var positionsDict = columnPositions.ToDictionary(p => p.ColumnId, p => p.Position);

        var updatedColumns = Board.Columns.Select(c =>
            positionsDict.TryGetValue(c.Id, out var newPos) ? c with { Position = newPos } : c
        ).OrderBy(c => c.Position).ToList();

        Board = Board with { Columns = updatedColumns };
    }

    private void ApplyColumnTaskPositions(Guid columnId, IReadOnlyList<BoardActionTaskPosition> taskPositions)
    {
        if (taskPositions == null || taskPositions.Count == 0)
        {
            return;
        }

        var positionsDict = taskPositions.ToDictionary(p => p.BoardTaskId, p => p.Position);

        for (int i = 0; i < Tasks.Count; i++)
        {
            var task = Tasks[i];
            if (task.ColumnId == columnId && positionsDict.TryGetValue(task.Id, out var newPosition))
            {
                Tasks[i] = task with { Position = newPosition };
            }
        }
    }
}
