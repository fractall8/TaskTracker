using Contracts.Messaging;

namespace TaskTracker.Functions.Interfaces;

public interface IBoardExportProcessor
{
    Task RunAsync(BoardExportMessage message, CancellationToken ct = default);
}
