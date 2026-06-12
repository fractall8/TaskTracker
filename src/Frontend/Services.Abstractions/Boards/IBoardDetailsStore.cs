using Contracts.DTOs;

namespace Services.Abstractions.Boards;

public interface IBoardDetailsStore
{
  Guid? BoardId { get; }

  BoardWithColumnsDto? Board { get; }

  bool IsLoading { get; }

  string? ErrorMessage { get; }
  
  event Action? StateChanged;
  
  void Reset();

  Task LoadAsync(Guid boardId, CancellationToken ct = default);

  Task<ColumnDto> CreateColumnAsync(string name, CancellationToken ct = default);

  Task UpdateColumnNameAsync(Guid columnId, string name, CancellationToken ct = default);

  Task DeleteColumnAsync(Guid columnId, CancellationToken ct = default);

  Task ReorderColumnsAsync(Guid columnId, int newPosition, CancellationToken ct = default);

  void UpdateBoardName(string name);
}