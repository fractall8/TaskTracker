using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Export;
using Contracts.Messaging;
using Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Boards.Jobs;

internal static class BoardExportJobOperations
{
    internal static async Task EnqueueAndMarkPendingAsync(
        IBoardExportQueueSender queueSender,
        BoardExportStatusInfoDto info,
        BoardExportType exportType,
        Func<Guid, CancellationToken, Task> markPendingAsync,
        Func<BoardExportStatusChangedNotification, CancellationToken, Task> notifyStatusChangedAsync,
        ILogger logger,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString();

        var options = exportType == BoardExportType.ReExport ? info.ReExportOptions : info.ExportOptions;

        var message = new BoardExportMessage(info.BoardId, options!, exportType, correlationId);

        try
        {
            await markPendingAsync(info.BoardId, ct);

            await queueSender.SendAsync(message, ct);

            await notifyStatusChangedAsync(
                new BoardExportStatusChangedNotification(info.BoardId, BoardExportStatusDto.Pending),
                ct);

            logger.LogInformation(
                "Enqueued board export. BoardId={BoardId}, ExportType={ExportType}, CorrelationId={CorrelationId}",
                info.BoardId,
                exportType,
                correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to enqueue board export. BoardId={BoardId}, ExportType={ExportType}, CorrelationId={CorrelationId}",
                info.BoardId,
                exportType,
                correlationId);
        }
    }
}
