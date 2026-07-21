using Contracts.DTOs;

namespace Contracts.Notifications.BoardExport;

public record BoardExportStatusChangedNotification(
    Guid BoardId,
    BoardExportStatusDto Status,
    BoardExportOptionsDto? ExportOptions = null);
