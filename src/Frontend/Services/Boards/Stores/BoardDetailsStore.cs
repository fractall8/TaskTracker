using Contracts.DTOs;
using Contracts.Requests.Tasks;
using Services.Abstractions.Boards;
using Services.Abstractions.Columns;
using Services.Abstractions.Tasks;

namespace Services.Boards.Stores;

public class BoardDetailsStore(IBoardApiService boardsApi, IColumnApiService columnsApi, ITaskApiService tasksApi)
    : IBoardDetailsStore
{
    public Guid? BoardId { get; private set; }

    public BoardWithColumnsDto? Board { get; private set; }

    public List<TaskDto> Tasks { get; private set; } = [];

    public bool IsLoading { get; private set; }

    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

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

    public void UpdateBoardName(string name)
    {
        if (Board != null)
        {
            Board = Board with { Name = name };
            NotifyStateChanged();
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

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            return;
        }

        await tasksApi.DeleteTaskAsync(BoardId.Value, taskId, ct);

        Tasks = Tasks.Where(t => t.Id != taskId).ToList();
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

        NotifyStateChanged();
    }

    public async Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int newPosition, CancellationToken ct = default)
    {
        if (BoardId == null)
        {
            return;
        }

        var taskToMove = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (taskToMove == null || (taskToMove.ColumnId == targetColumnId && taskToMove.Position == newPosition))
        {
            return;
        }

        var oldColumnId = taskToMove.ColumnId;
        var oldPosition = taskToMove.Position;

        Tasks = Tasks.Select(t =>
            {
                if (t.Id == taskId)
                {
                    return t with { ColumnId = targetColumnId, Position = newPosition };
                }

                if (oldColumnId == targetColumnId)
                {
                    if (t.ColumnId == targetColumnId)
                    {
                        if (oldPosition < newPosition && t.Position > oldPosition && t.Position <= newPosition)
                        {
                            return t with { Position = t.Position - 1 };
                        }

                        if (oldPosition > newPosition && t.Position >= newPosition && t.Position < oldPosition)
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

                if (t.ColumnId == targetColumnId && t.Position >= newPosition)
                {
                    return t with { Position = t.Position + 1 };
                }

                return t;
            })
            .OrderBy(t => t.Position)
            .ToList();

        NotifyStateChanged();

        try
        {
            var request = new MoveTaskRequest(targetColumnId, newPosition);
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
}
