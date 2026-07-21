using Contracts.Notifications.BoardExport;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Services.Abstractions.Boards;
using Services.Abstractions.Hubs;
using Services.Configuration;

namespace Services.Hubs;

public class BoardExportStatusHubService(
    IAccessTokenProvider tokenProvider,
    IOptions<ApiClientOptions> options,
    IBoardDetailsStore boardDetailsStore)
    : IBoardExportStatusHubService, IAsyncDisposable
{
    private readonly ApiClientOptions _options = options.Value;
    private HubConnection? _connection;
    private Guid _currentBoardId;

    public async Task ConnectAsync(Guid boardId, CancellationToken ct = default)
    {
        if (_connection?.State == HubConnectionState.Connected && _currentBoardId == boardId)
        {
            return;
        }

        await DisconnectAsync(ct);
        _currentBoardId = boardId;

        var hubUrl = $"{_options.BaseUrl.TrimEnd('/')}/hubs/board-export-status";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var result = await tokenProvider.RequestAccessToken(new AccessTokenRequestOptions { Scopes = _options.Scopes });
                    return result.TryGetToken(out var token) ? token.Value : null;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<BoardExportStatusChangedNotification>("BoardExportStatusChanged", async notification =>
        {
            if (notification.BoardId == _currentBoardId)
            {
                await boardDetailsStore.LoadAsync(notification.BoardId, ct: ct);
            }
        });

        _connection.On<BoardExportStatusChangedNotification>("BoardReExportStatusChanged", async notification =>
        {
            if (notification.BoardId == _currentBoardId)
            {
                await boardDetailsStore.LoadAsync(notification.BoardId, ct: ct);
            }
        });

        await _connection.StartAsync(ct);

        try
        {
            await _connection.InvokeAsync("SubscribeAsync", new[] { boardId }, ct);
        }
        catch { }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            try { await _connection.InvokeAsync("UnsubscribeAsync", new[] { _currentBoardId }, ct); } catch { }
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
