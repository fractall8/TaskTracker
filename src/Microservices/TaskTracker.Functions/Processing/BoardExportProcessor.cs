using Contracts.Messaging;
using Microsoft.Extensions.Logging;
using TaskTracker.Functions.Interfaces;

namespace TaskTracker.Functions.Processing;

public class BoardExportProcessor(
    IBoardExportDataApiClient apiClient,
    ILogger<BoardExportProcessor> logger) : IBoardExportProcessor
{
    public async Task RunAsync(BoardExportMessage message, CancellationToken ct = default)
    {
        logger.LogInformation("Step 1: Fetching board data from main API for BoardId={BoardId}...", message.BoardId);

        var exportData = await apiClient.GetExportDataAsync(message.BoardId, message.ExportOptions, ct);

        logger.LogInformation("Step 2: Building archive in memory...");

        // TODO: await using var archiveStream = await archiveBuilder.BuildAsync(exportData, ct);

        logger.LogInformation("Step 3: Uploading archive to Blob Storage (TODO)...");
        // TODO: Implement in this PR exportBlobService.UploadArchiveAsync

        logger.LogInformation("Step 4: Updating CosmosDB status and notifying UI (TODO)...");
        // TODO: Implement in this PR completionHandler.MarkCompletedAsync

        logger.LogInformation("Export processing successfully completed in memory for BoardId={BoardId}", message.BoardId);
    }
}
