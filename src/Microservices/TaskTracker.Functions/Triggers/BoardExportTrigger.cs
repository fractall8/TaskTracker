using Contracts.Messaging;
using Microsoft.Extensions.Logging;
using TaskTracker.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Contracts.Constants;

namespace TaskTracker.Functions.Triggers;

public class BoardExportQueueTrigger(
    ILogger<BoardExportQueueTrigger> logger,
    IBoardExportProcessor boardExportProcessor)
{
    [Function(nameof(BoardExportQueueTrigger))]
    public async Task Run(
        [ServiceBusTrigger(ServiceBusQueueNames.BoardArchivingQueue)]
        BoardExportMessage message,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Board export started. BoardId={BoardId}, ExportType={ExportType}, CorrelationId={CorrelationId}",
            message.BoardId,
            message.ExportType,
            message.CorrelationId);

        try
        {
            await boardExportProcessor.RunAsync(message, ct);

            logger.LogInformation(
                "Board export finished. BoardId={BoardId}, ExportType={ExportType}, CorrelationId={CorrelationId}",
                message.BoardId,
                message.ExportType,
                message.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Board export failed. BoardId={BoardId}, ExportType={ExportType}, CorrelationId={CorrelationId}",
                message.BoardId,
                message.ExportType,
                message.CorrelationId);

            throw;
        }
    }
}
