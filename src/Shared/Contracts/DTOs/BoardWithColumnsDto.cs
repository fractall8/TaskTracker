namespace Contracts.DTOs;

public record BoardWithColumnsDto(
    Guid Id,
    string Name,
    string? Description,
    IEnumerable<ColumnDto> Columns);
