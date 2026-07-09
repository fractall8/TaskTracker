using Contracts.Messaging;

namespace Application.Interfaces.Services;

public interface IBoardExportQueueSender
{
    Task SendAsync(BoardExportMessage message, CancellationToken ct = default);
}
