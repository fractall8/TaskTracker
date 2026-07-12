using Contracts.DTOs;
using Contracts.Export;

namespace Contracts.Messaging;

public record BoardExportMessage(
    Guid BoardId,
    BoardExportOptionsDto ExportOptions,
    BoardExportType ExportType,
    string CorrelationId);
