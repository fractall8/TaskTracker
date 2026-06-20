using Contracts.DTOs;
using Contracts.Requests;

namespace Services.Abstractions.Boards;

public interface IBoardDetailsStore
{
    Guid? BoardId { get; }

    BoardWithColumnsDto? Board { get; }

    List<TaskDto> Tasks { get; }

    bool IsLoading { get; }

    string? ErrorMessage { get; }

    event Action? StateChanged;

    void Reset();

    Task LoadAsync(Guid boardId, string? searchTerm = null, CancellationToken ct = default);

    Task<ColumnDto> CreateColumnAsync(string name, CancellationToken ct = default);

    Task UpdateColumnNameAsync(Guid columnId, string name, CancellationToken ct = default);

    Task DeleteColumnAsync(Guid columnId, CancellationToken ct = default);

    Task ReorderColumnsAsync(Guid columnId, int newPosition, CancellationToken ct = default);

    void UpdateBoardName(string name);

    Task UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);

    Task DeleteTaskAsync(Guid taskId, CancellationToken ct = default);

    Task CreateTaskAsync(Guid columnId, CreateTaskRequest request, CancellationToken ct = default);

    Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int newPosition, CancellationToken ct = default);
}
