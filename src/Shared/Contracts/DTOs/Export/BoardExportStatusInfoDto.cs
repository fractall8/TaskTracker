namespace Contracts.DTOs;

public record BoardExportStatusInfoDto(
    Guid BoardId,
    DateTimeOffset UpdatedAtUtc,
    BoardExportStatusDto ExportStatus,
    BoardExportOptionsDto? ExportOptions,
    BoardExportStatusDto ReExportStatus = BoardExportStatusDto.None,
    BoardExportOptionsDto? ReExportOptions = null,
    string? ErrorMessage = null);
