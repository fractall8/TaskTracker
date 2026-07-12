using Contracts.Messaging;
using Microsoft.Extensions.Logging;
using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Models;
using TaskTracker.Functions.Processing.Export;

namespace TaskTracker.Functions.Processing;

public class BoardExportProcessor(
    ExportContextResolver exportContextResolver,
    BoardExportCompletionHandlerRegistry completionHandlerRegistry,
    IBoardExportDocumentClient boardExportDocumentClient,
    IBoardExportDataApiClient boardExportDataApiClient,
    IBoardArchiveBuilder archiveBuilder,
    IBoardExportBlobService exportBlobService,
    ILogger<BoardExportProcessor> logger)
    : IBoardExportProcessor
{
    public async Task RunAsync(BoardExportMessage message, CancellationToken ct = default)
    {
        var exportInfo = await boardExportDocumentClient.GetBoardExportInfoAsync(message.BoardId, ct)
                         ?? throw new InvalidOperationException($"Export document not found for board {message.BoardId}.");

        // get an appropriate export policy (for initial export or re-export) according to BoardExportMessage type
        var exportContext = exportContextResolver.Resolve(message, exportInfo);
        var completionHandler = completionHandlerRegistry.Get(message.ExportType);

        if (exportContext.ShouldSkip)
        {
            logger.LogInformation(
                "Skipping export. BoardId={BoardId}, Type={ExportType}, Reason={Reason}",
                message.BoardId,
                message.ExportType,
                exportContext.SkipReason);

            return;
        }

        if (exportContext.Options is not { } exportOptions)
        {
            throw new InvalidOperationException(
                $"Export options are missing for board {exportContext.BoardId}.");
        }

        try
        {
            await completionHandler.MarkProcessingAsync(exportContext, ct);

            var exportData = await boardExportDataApiClient.GetExportDataAsync(
                exportContext.BoardId,
                exportOptions,
                ct);

            // TODO: resolve summary formats from exportOptions when PDF export is supported.
            IReadOnlyList<BoardExportSummaryFormat> summaryFormats = [BoardExportSummaryFormat.Json];

            await using var archive = await archiveBuilder.BuildAsync(exportData, summaryFormats, ct);

            await exportBlobService.UploadArchiveAsync(exportContext.BoardId, archive, ct);

            await completionHandler.MarkCompletedAsync(exportContext, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Board export failed. BoardId={BoardId}, ExportType={ExportType}",
                message.BoardId,
                message.ExportType);

            await completionHandler.MarkFailedAsync(exportContext, ex.Message, ct);

            throw;
        }
    }
}
