using System.Text.Json;
using Application.Interfaces.Services;
using Azure.Messaging.ServiceBus;
using Contracts.Messaging;

namespace Infrastructure.Boards.Export;

internal sealed class AzureBoardExportQueueSender(ServiceBusSender sender) : IBoardExportQueueSender
{
    public async Task SendAsync(BoardExportMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            MessageId = message.CorrelationId,
        };

        await sender.SendMessageAsync(sbMessage, ct);
    }
}
