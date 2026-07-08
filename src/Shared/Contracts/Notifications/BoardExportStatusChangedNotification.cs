using Contracts.DTOs;

namespace Contracts.Notifications;

public record BoardExportStatusChangedNotification(
    Guid BoardId,
    BoardExportStatusDto Status,
    BoardExportOptionsDto? ExportOptions = null);
