using Contracts.Export;
using TaskTracker.Functions.Processing.Export;

namespace TaskTracker.Functions.Interfaces;

public interface IBoardExportCompletionHandler
{
    BoardExportType Type { get; }

    Task MarkProcessingAsync(BoardExportContext context, CancellationToken ct = default);

    Task MarkCompletedAsync(BoardExportContext context, CancellationToken ct = default);

    Task MarkFailedAsync(BoardExportContext context, string errorMessage, CancellationToken ct = default);
}
