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
    BoardExportStatusDto? BoardExportStatus = BoardExportStatusDto.None,
    BoardExportStatusDto? BoardReExportStatus = BoardExportStatusDto.None,
    BoardExportOptionsDto? ExportOptions = null);
