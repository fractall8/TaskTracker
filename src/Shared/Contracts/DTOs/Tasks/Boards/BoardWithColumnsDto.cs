using Contracts.Enums;

namespace Contracts.DTOs;

public record BoardWithColumnsDto(
    Guid Id,
    string Name,
    string? Description,
    Guid WorkspaceId,
    BoardRoleDto BoardRole,
    IEnumerable<ColumnDto> Columns);
