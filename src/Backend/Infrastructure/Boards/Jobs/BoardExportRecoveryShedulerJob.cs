using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Boards.Jobs;

[AutomaticRetry(Attempts = 0)]
internal sealed class BoardExportRecoverySchedulerJob(
    IBoardExportService boardExportService,
    IBoardExportQueueSender queueSender,
    IBoardExportStatusNotifier exportStatusNotifier,
    IOptions<BoardExportRecoverySchedulerOptions> settings,
    IDateTimeProvider dateTimeProvider,
    ILogger<BoardExportRecoverySchedulerJob> logger) : IBoardExportRecoverySchedulerJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var options = settings.Value;

        var failedCooldownThreshold = dateTimeProvider.UtcNow.AddMinutes(-options.FailedRetryCooldownMinutes);
        var staleCooldownThreshold = dateTimeProvider.UtcNow.AddMinutes(-options.StaleCooldownMinutes);

        logger.LogInformation(
            "Board export recovery scheduler started. BatchSize={BatchSize}, FailedCooldownThreshold={FailedCooldownThreshold:O}, StaleCooldownThreshold={StaleCooldownThreshold:O}",
            options.DocumentBatchSize,
            failedCooldownThreshold,
            staleCooldownThreshold);

        var processed = 0;

        await foreach (var info in boardExportService.ScanForFailedExportStatusesAsync(
                           options.DocumentBatchSize,
                           failedCooldownThreshold,
                           ct))
        {
            await RecoverExportAsync(info, ct);
            processed++;
        }

        await foreach (var info in boardExportService.ScanForStaleExportStatusesAsync(
                           options.DocumentBatchSize,
                           staleCooldownThreshold,
                           ct))
        {
            await RecoverExportAsync(info, ct);
            processed++;
        }

        logger.LogInformation("Board export recovery scheduler finished. Processed={Processed}", processed);
    }

    private async Task RecoverExportAsync(BoardExportStatusInfoDto info, CancellationToken ct)
    {
        if (ShouldRecover(info.ExportStatus))
        {
            await BoardExportJobOperations.EnqueueAndMarkPendingAsync(
                queueSender,
                info,
                false,
                (boardId, token) => boardExportService.UpdateExportStatusAsync(boardId, BoardExportStatusDto.Pending, null, token),
                exportStatusNotifier.NotifyExportStatusChangedAsync,
                logger,
                ct);
        }

        if (ShouldRecover(info.ReExportStatus))
        {
            await BoardExportJobOperations.EnqueueAndMarkPendingAsync(
                queueSender,
                info,
                true,
                (boardId, token) => boardExportService.UpdateReExportStatusAsync(boardId, BoardExportStatusDto.Pending, null, token),
                exportStatusNotifier.NotifyReExportStatusChangedAsync,
                logger,
                ct);
        }
    }

    private static bool ShouldRecover(BoardExportStatusDto status) =>
        status is BoardExportStatusDto.Pending or BoardExportStatusDto.Processing or BoardExportStatusDto.Failed;
}
