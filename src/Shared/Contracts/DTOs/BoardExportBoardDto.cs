namespace Contracts.DTOs;

public record BoardExportBoardDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    bool IsArchived,
    DateTimeOffset? ArchivedAt,
    int ColumnCount,
    int TaskCount);
