namespace Contracts.DTOs;

public record BoardExportColumnDto(
    Guid Id,
    string Name,
    int Position,
    IReadOnlyList<BoardExportTaskDto> Tasks);
