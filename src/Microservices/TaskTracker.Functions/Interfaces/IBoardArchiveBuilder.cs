using Contracts.DTOs;
using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.Interfaces;

public interface IBoardArchiveBuilder
{
    Task<BoardExportArchive> BuildAsync(
        BoardExportDataDto data,
        IReadOnlyList<BoardExportSummaryFormat> summaryFormats,
        CancellationToken ct = default);
}
