using System.Net;
using System.Runtime.CompilerServices;
using Application.Common.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Infrastructure.Boards.Export;
using Microsoft.Azure.Cosmos;

namespace Infrastructure.Services;

internal sealed class CosmosBoardExportService(Container container, IDateTimeProvider dateTimeProvider) : IBoardExportService
{
    public async Task SetExportAsync(Guid boardId, BoardExportStatusDto exportStatus, BoardExportOptionsDto exportOptions, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(exportOptions);

        var document = new BoardExportDocument
        {
            Id = boardId.ToString(),
            BoardId = boardId,
            ExportStatus = (int)exportStatus,
            ExportStatusName = exportStatus.ToString(),
            UpdatedAtUtc = dateTimeProvider.UtcNow,
            ErrorMessage = null,
            ExportOptions = exportOptions,
        };

        await container.UpsertItemAsync(
            document,
            new PartitionKey(boardId.ToString()),
            cancellationToken: ct);
    }

    public async Task SetReExportAsync(Guid boardId, BoardExportStatusDto reExportStatus, BoardExportOptionsDto reExportOptions, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reExportOptions);

        await container.PatchItemAsync<BoardExportDocument>(
            boardId.ToString(),
            new PartitionKey(boardId.ToString()),
            [
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusJson}", (int)reExportStatus),
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusNameJson}", reExportStatus.ToString()),
                PatchOperation.Set($"/{BoardExportDocument.ReExportOptionsJson}", reExportOptions),
            ],
            cancellationToken: ct);
    }

    public async Task UpdateExportStatusAsync(Guid boardId, BoardExportStatusDto status, string? errorMessage = null, CancellationToken ct = default)
    {
        await container.PatchItemAsync<BoardExportDocument>(
            boardId.ToString(),
            new PartitionKey(boardId.ToString()),
            [
                PatchOperation.Set($"/{BoardExportDocument.ExportStatusJson}", (int)status),
                PatchOperation.Set($"/{BoardExportDocument.ExportStatusNameJson}", status.ToString()),
                PatchOperation.Set($"/{BoardExportDocument.UpdatedAtUtcJson}", dateTimeProvider.UtcNow),
                PatchOperation.Set($"/{BoardExportDocument.ErrorMessageJson}", errorMessage),
            ],
            cancellationToken: ct);
    }

    public async Task<BoardExportStatusInfoDto?> GetBoardExportInfoAsync(Guid boardId, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);

        try
        {
            var response = await container.ReadItemAsync<BoardExportDocument>(
                boardId.ToString(),
                new PartitionKey(boardId.ToString()),
                cancellationToken: ct);

            return ToInfo(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<Guid, BoardExportStatusInfoDto>> GetBoardListExportInfoAsync(IReadOnlyCollection<Guid> boardIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(boardIds);

        if (boardIds.Count == 0)
        {
            return new Dictionary<Guid, BoardExportStatusInfoDto>();
        }

        var itemIds = boardIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Select(id => (id.ToString(), new PartitionKey(id.ToString())))
            .ToList();

        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, BoardExportStatusInfoDto>();
        }

        var response = await container.ReadManyItemsAsync<BoardExportDocument>(
            itemIds,
            cancellationToken: ct);

        return response
            .Select(ToInfo)
            .ToDictionary(status => status.BoardId);
    }

    public async IAsyncEnumerable<BoardExportStatusInfoDto> ScanForRequestedExportStatusesAsync(
        int maxDocuments,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.exportStatus = @requested OR c.reExportStatus = @requested")
            .WithParameter("@requested", (int)BoardExportStatusDto.Requested);

        await foreach (var info in ScanAsync(query, maxDocuments, ct))
        {
            yield return info;
        }
    }

    public async IAsyncEnumerable<BoardExportStatusInfoDto> ScanForFailedExportStatusesAsync(
        int maxDocuments,
        DateTime failedCooldownThreshold,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE " +
            "(c.exportStatus = @failed AND c.updatedAtUtc <= @cooldown) " +
            "OR (c.reExportStatus = @failed AND c.updatedAtUtc <= @cooldown)")
            .WithParameter("@failed", (int)BoardExportStatusDto.Failed)
            .WithParameter("@cooldown", failedCooldownThreshold);

        await foreach (var info in ScanAsync(query, maxDocuments, ct))
        {
            yield return info;
        }
    }

    public async IAsyncEnumerable<BoardExportStatusInfoDto> ScanForStaleExportStatusesAsync(
        int maxDocuments,
        DateTime staleCooldownThreshold,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE " +
            "(c.exportStatus = @pending AND c.updatedAtUtc <= @staleThreshold) " +
            "OR (c.exportStatus = @processing AND c.updatedAtUtc <= @staleThreshold) " +
            "OR (c.reExportStatus = @pending AND c.updatedAtUtc <= @staleThreshold) " +
            "OR (c.reExportStatus = @processing AND c.updatedAtUtc <= @staleThreshold)")
            .WithParameter("@pending", (int)BoardExportStatusDto.Pending)
            .WithParameter("@processing", (int)BoardExportStatusDto.Processing)
            .WithParameter("@staleThreshold", staleCooldownThreshold);

        await foreach (var info in ScanAsync(query, maxDocuments, ct))
        {
            yield return info;
        }
    }

    private async IAsyncEnumerable<BoardExportStatusInfoDto> ScanAsync(QueryDefinition query, int maxDocuments, [EnumeratorCancellation] CancellationToken ct)
    {
        using var iterator = container.GetItemQueryIterator<BoardExportDocument>(
            query,
            requestOptions: new QueryRequestOptions { MaxItemCount = maxDocuments });

        var fetched = 0;
        while (iterator.HasMoreResults && fetched < maxDocuments)
        {
            ct.ThrowIfCancellationRequested();
            var page = await iterator.ReadNextAsync(ct);
            foreach (var document in page)
            {
                yield return ToInfo(document);
                if (++fetched >= maxDocuments)
                {
                    yield break;
                }
            }
        }
    }

    public async Task UpdateReExportStatusAsync(Guid boardId, BoardExportStatusDto reExportStatus, string? errorMessage = null, CancellationToken ct = default)
    {
        await container.PatchItemAsync<BoardExportDocument>(
            boardId.ToString(),
            new PartitionKey(boardId.ToString()),
            [
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusJson}", (int)reExportStatus),
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusNameJson}", reExportStatus.ToString()),
                PatchOperation.Set($"/{BoardExportDocument.UpdatedAtUtcJson}", dateTimeProvider.UtcNow),
                PatchOperation.Set($"/{BoardExportDocument.ErrorMessageJson}", errorMessage),
            ],
            cancellationToken: ct);
    }

    private static BoardExportStatusInfoDto ToInfo(BoardExportDocument document)
    {
        return new BoardExportStatusInfoDto(document.BoardId,
            document.UpdatedAtUtc,
            (BoardExportStatusDto)document.ExportStatus,
            document.ExportOptions,
            document.ReExportStatus is { } reExportStatus
                ? (BoardExportStatusDto)reExportStatus
                : BoardExportStatusDto.None,
            document.ReExportOptions,
            document.ErrorMessage);
    }
}
