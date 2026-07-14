using Contracts.Enums;

namespace Contracts.DTOs;

public record BoardWithColumnsDto(
    Guid Id,
    string Name,
    string? Description,
    Guid WorkspaceId,
    BoardRoleDto BoardRole,
    IEnumerable<ColumnDto> Columns,
    bool IsArchived = false,
    BoardExportStatusDto? ExportStatus = BoardExportStatusDto.None,
    BoardExportStatusDto? ReExportStatus = BoardExportStatusDto.None,
    BoardExportOptionsDto? ExportOptions = null,
    BoardExportOptionsDto? ReExportOptions = null);
