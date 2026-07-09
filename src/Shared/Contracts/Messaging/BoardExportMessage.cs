using Contracts.DTOs;

namespace Contracts.Messaging;

public record BoardExportMessage(
    Guid BoardId,
    BoardExportOptionsDto ExportOptions,
    bool IsReExport,
    string CorrelationId);
