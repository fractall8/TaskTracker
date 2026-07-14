using Contracts.DTOs;
using Contracts.Export;

namespace TaskTracker.Functions.Processing.Export;

public record BoardExportContext(
    Guid BoardId,
    BoardExportType Type,
    BoardExportOptionsDto? Options,
    bool ShouldSkip = false,
    string? SkipReason = null);
