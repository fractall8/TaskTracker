namespace Services.Abstractions.Hubs;

public interface IBoardActionsHubService
{
    Task ConnectAsync(Guid boardId, CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);
}
