using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.Archiving;

public sealed class BoardExportSummaryWriterRegistry
{
    private readonly IReadOnlyDictionary<BoardExportSummaryFormat, IBoardExportSummaryWriter> _writers;

    public BoardExportSummaryWriterRegistry(IEnumerable<IBoardExportSummaryWriter> writers)
    {
        ArgumentNullException.ThrowIfNull(writers);

        _writers = writers.ToDictionary(w => w.Format);
    }

    public IBoardExportSummaryWriter Get(BoardExportSummaryFormat format) =>
        _writers.TryGetValue(format, out var writer)
            ? writer
            : throw new InvalidOperationException($"No summary writer registered for {format}.");
}
