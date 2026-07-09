using Application.Interfaces.Notifiers;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Boards.Jobs;

[AutomaticRetry(Attempts = 0)]
internal sealed class BoardExportSchedulerJob(
    IBoardExportService boardExportService,
    IBoardExportQueueSender queueSender,
    IBoardExportStatusNotifier exportStatusNotifier,
    IOptions<BoardExportSchedulerOptions> settings,
    ILogger<BoardExportSchedulerJob> logger) : IBoardExportSchedulerJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var options = settings.Value;

        logger.LogInformation(
            "Board export scheduler started. BatchSize={BatchSize}",
            options.DocumentBatchSize);

        var processed = 0;

        await foreach (var info in
                       boardExportService.ScanForRequestedExportStatusesAsync(options.DocumentBatchSize, ct))
        {
            if (info.ExportStatus == BoardExportStatusDto.Requested)
            {
                await BoardExportJobOperations.EnqueueAndMarkPendingAsync(
                    queueSender,
                    info,
                    false,
                    (boardId, token) =>
                        boardExportService.UpdateExportStatusAsync(boardId, BoardExportStatusDto.Pending, null, token),
                    exportStatusNotifier.NotifyExportStatusChangedAsync,
                    logger,
                    ct);
            }

            if (info.ReExportStatus == BoardExportStatusDto.Requested)
            {
                await BoardExportJobOperations.EnqueueAndMarkPendingAsync(
                    queueSender,
                    info,
                    true,
                    (boardId, token) =>
                        boardExportService.UpdateReExportStatusAsync(boardId, BoardExportStatusDto.Pending, null,
                            token),
                    exportStatusNotifier.NotifyExportStatusChangedAsync,
                    logger,
                    ct);
            }

            processed++;
        }

        logger.LogInformation("Board export scheduler finished. Processed={Processed}", processed);
    }
}
