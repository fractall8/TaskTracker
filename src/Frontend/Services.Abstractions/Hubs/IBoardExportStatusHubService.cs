namespace Services.Abstractions.Hubs;

public interface IBoardExportStatusHubService
{
    Task ConnectAsync(Guid boardId, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
