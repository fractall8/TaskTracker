using System.Text.Json.Serialization.Metadata;
using Contracts.Notifications.BoardActions;
using Contracts.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Abstractions.Auth;
using Services.Abstractions.BoardCalls;
using Services.Abstractions.Boards;
using Services.Abstractions.Hubs;
using Services.Abstractions.Tasks;
using Services.Configuration;

namespace Services.Hubs;

public class BoardActionsHubService(
    IAccessTokenProvider tokenProvider,
    IOptions<ApiClientOptions> options,
    IBoardDetailsStore boardDetailsStore,
    ITaskDetailsStore taskDetailsStore,
    IBoardCallStore boardCallStore,
    IProfileStore profileStore,
    ILogger<BoardActionsHubService> logger)
    : IBoardActionsHubService, IAsyncDisposable
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

        var hubUrl = $"{_options.BaseUrl.TrimEnd('/')}/hubs/board-actions";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
            {
                opts.AccessTokenProvider = async () =>
                {
                    var result = await tokenProvider.RequestAccessToken(new AccessTokenRequestOptions
                        { Scopes = _options.Scopes });
                    return result.TryGetToken(out var token) ? token.Value : null;
                };
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicJsonModifier.AddBoardActionPolymorphism }
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<BoardActionNotification>("BoardChanged", notification =>
        {
            var currentUserId = profileStore.Profile?.Id ?? Guid.Empty;
            boardDetailsStore.ApplyAction(notification, currentUserId);
            taskDetailsStore.ApplyAction(notification, currentUserId);
            boardCallStore.ApplyAction(notification, currentUserId);
        });

        const int maxRetries = 5;
        int retryCount = 0;
        bool isConnected = false;

        while (retryCount < maxRetries && !ct.IsCancellationRequested)
        {
            try
            {
                await _connection.StartAsync(ct);
                isConnected = true;
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex, "[SignalR] Initial connection failed. Retry {RetryCount}/{MaxRetries}",
                    retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    logger.LogError("[SignalR] Could not connect after {MaxRetries} attempts.", maxRetries);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 5)), ct);
            }
        }

        if (isConnected)
        {
            try
            {
                await _connection.InvokeAsync("SubscribeAsync", boardId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError("[SignalR] Failed to subscribe to BoardActions: {Message}", ex.Message);
            }
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.InvokeAsync("UnsubscribeAsync", _currentBoardId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError("Error during disconect: {Message}", ex.Message);
            }

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
