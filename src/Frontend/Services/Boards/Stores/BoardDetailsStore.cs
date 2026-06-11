using Contracts.DTOs;
using Services.Abstractions.Boards;
using Services.Abstractions.Columns;

namespace Services.Boards.Stores;

public class BoardDetailsStore(IBoardApiService boardApi, IColumnApiService columnsApi) : IBoardDetailsStore
{
    public Guid? BoardId { get; private set; }
    public BoardWithColumnsDto? Board { get; private set; }
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
    }

    public async Task LoadAsync(Guid boardId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var loadedBoard = await boardApi.GetBoardByIdAsync(boardId, ct);
            
            BoardId = boardId;
            Board = loadedBoard with 
            { 
                Columns = loadedBoard.Columns.OrderBy(c => c.Position).ToList() 
            };
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
        if (BoardId == null || Board == null) return;

        var column = Board.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null || column.Name == name) return;

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
        if (BoardId == null || Board == null) return;

        var columnToDelete = Board.Columns.FirstOrDefault(c => c.Id == columnId);
        if (columnToDelete == null) return;

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
        if (BoardId == null || Board == null) return;

        await columnsApi.MoveColumnAsync(BoardId.Value, columnId, newPosition, ct);
        
        await LoadAsync(BoardId.Value, ct); 
    }

    public void UpdateBoardName(string name)
    {
        if (Board != null)
        {
            Board = Board with { Name = name };
            NotifyStateChanged();
        }
    }
}