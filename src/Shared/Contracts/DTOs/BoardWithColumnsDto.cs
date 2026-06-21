namespace Contracts.DTOs;

public record BoardWithColumnsDto(
    Guid Id,
    string Name,
    string? Description,
    Guid WorkspaceId,
    IEnumerable<ColumnDto> Columns);
