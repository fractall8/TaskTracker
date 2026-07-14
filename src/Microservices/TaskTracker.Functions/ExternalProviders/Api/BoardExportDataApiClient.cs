using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Contracts.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Functions.Interfaces;

namespace TaskTracker.Functions.ExternalProviders.Api;

public sealed class BoardExportDataApiClient(
    HttpClient httpClient,
    IOptions<BoardExportApiClientOptions> options,
    ILogger<BoardExportDataApiClient> logger) : IBoardExportDataApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BoardExportDataDto> GetExportDataAsync(Guid boardId, BoardExportOptionsDto exportOptions,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(exportOptions);

        var clientOptions = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/boards/{boardId:D}/export-data");
        request.Content = JsonContent.Create(exportOptions, options: _jsonOptions);

        request.Headers.TryAddWithoutValidation(clientOptions.ApiKeyHeaderName, clientOptions.ApiKey);

        using var response = await httpClient.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Board {boardId} was not found for export.");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException($"Board {boardId} is not eligible for export.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            logger.LogError(
                "Export data request failed. BoardId={BoardId}, StatusCode={StatusCode}, Body={Body}",
                boardId,
                (int)response.StatusCode,
                body);

            response.EnsureSuccessStatusCode();
        }

        var data = await response.Content.ReadFromJsonAsync<BoardExportDataDto>(options: _jsonOptions,
            cancellationToken: ct);

        if (data == null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize export data for board {boardId}. API returned empty or invalid JSON.");
        }

        if (data.Board == null || data.Board.Id != boardId)
        {
            throw new InvalidOperationException(
                $"Export data board id mismatch. Expected {boardId}, got {data.Board?.Id}.");
        }

        return data;
    }
}
