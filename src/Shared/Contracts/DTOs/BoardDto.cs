using Contracts.Enums;

namespace Contracts.DTOs;

public record BoardDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    BoardRoleDto UserRole,
    IEnumerable<UserWithRoleDto> Members,
    IEnumerable<ColumnDto> Columns);