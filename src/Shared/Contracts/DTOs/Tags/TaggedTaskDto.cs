namespace Contracts.DTOs;

// A tag is workspace-wide, so a task carrying it can live on any board: the board is part of the answer.
public record TaggedTaskDto(
    Guid TaskId,
    string Title,
    bool IsCompleted,
    DateTimeOffset? DueDate,
    Guid BoardId,
    string BoardName,
    bool IsBoardArchived,
    string ColumnName);
