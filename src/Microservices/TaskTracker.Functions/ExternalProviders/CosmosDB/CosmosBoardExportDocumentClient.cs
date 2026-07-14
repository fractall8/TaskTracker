using System.Net;
using Contracts.DTOs;
using Microsoft.Azure.Cosmos;
using Contracts.Export;
using TaskTracker.Functions.Interfaces;

namespace TaskTracker.Functions.ExternalProviders.CosmosDB;

public sealed class CosmosBoardExportDocumentClient(Container container) : IBoardExportDocumentClient
{
    public async Task<BoardExportStatusInfoDto?> GetBoardExportInfoAsync(Guid boardId, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);

        try
        {
            var response = await container.ReadItemAsync<BoardExportDocument>(
                boardId.ToString(),
                ToPartitionKey(boardId),
                cancellationToken: ct);

            return ToInfo(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task MarkExportProcessingAsync(Guid boardId, CancellationToken ct = default) =>
        PatchExportStatusAsync(boardId, BoardExportStatusDto.Processing, errorMessage: null, ct);

    public Task CompleteInitialExportAsync(Guid boardId, CancellationToken ct = default) =>
        PatchExportStatusAsync(boardId, BoardExportStatusDto.Completed, errorMessage: null, ct);

    public Task FailInitialExportAsync(Guid boardId, string errorMessage, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return PatchExportStatusAsync(boardId, BoardExportStatusDto.Failed, errorMessage, ct);
    }

    public Task MarkReExportProcessingAsync(Guid boardId, CancellationToken ct = default) =>
        PatchReExportStatusAsync(boardId, BoardExportStatusDto.Processing, ct);

    public async Task CompleteReExportAsync(Guid boardId, BoardExportOptionsDto promotedExportOptions, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(promotedExportOptions);

        var none = BoardExportStatusDto.None;

        await container.PatchItemAsync<BoardExportDocument>(
            boardId.ToString(),
            ToPartitionKey(boardId),
            [
                PatchOperation.Set($"/{BoardExportDocument.ExportOptionsJson}", promotedExportOptions),
                PatchOperation.Set($"/{BoardExportDocument.ExportStatusJson}", (int)BoardExportStatusDto.Completed),
                PatchOperation.Set($"/{BoardExportDocument.ExportStatusNameJson}", BoardExportStatusDto.Completed.ToString()),
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusJson}", (int)none),
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusNameJson}", none.ToString()),
                PatchOperation.Set<BoardExportOptionsDto?>($"/{BoardExportDocument.ReExportOptionsJson}", null),
                PatchOperation.Set($"/{BoardExportDocument.UpdatedAtUtcJson}", DateTime.UtcNow),
                PatchOperation.Set<string?>($"/{BoardExportDocument.ErrorMessageJson}", null),
            ],
            cancellationToken: ct);
    }

    public Task FailReExportAsync(Guid boardId, string errorMessage, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return PatchReExportStatusAsync(boardId, BoardExportStatusDto.Failed, ct, errorMessage);
    }

    private async Task PatchExportStatusAsync(Guid boardId, BoardExportStatusDto status, string? errorMessage, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);

        await container.PatchItemAsync<BoardExportDocument>(
            boardId.ToString(),
            ToPartitionKey(boardId),
            [
                PatchOperation.Set($"/{BoardExportDocument.ExportStatusJson}", (int)status),
                PatchOperation.Set($"/{BoardExportDocument.ExportStatusNameJson}", status.ToString()),
                PatchOperation.Set($"/{BoardExportDocument.UpdatedAtUtcJson}", DateTime.UtcNow),
                PatchOperation.Set($"/{BoardExportDocument.ErrorMessageJson}", errorMessage),
            ],
            cancellationToken: ct);
    }

    private async Task PatchReExportStatusAsync(Guid boardId, BoardExportStatusDto status, CancellationToken ct, string? errorMessage = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);

        await container.PatchItemAsync<BoardExportDocument>(
            boardId.ToString(),
            ToPartitionKey(boardId),
            [
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusJson}", (int)status),
                PatchOperation.Set($"/{BoardExportDocument.ReExportStatusNameJson}", status.ToString()),
                PatchOperation.Set($"/{BoardExportDocument.UpdatedAtUtcJson}", DateTime.UtcNow),
                PatchOperation.Set($"/{BoardExportDocument.ErrorMessageJson}", errorMessage),
            ],
            cancellationToken: ct);
    }

    private static PartitionKey ToPartitionKey(Guid boardId) => new(boardId.ToString());

    private static BoardExportStatusInfoDto ToInfo(BoardExportDocument document) =>
        new(
            document.BoardId,
            document.UpdatedAtUtc,
            (BoardExportStatusDto)document.ExportStatus,
            document.ExportOptions,
            document.ReExportStatus is { } reExportStatus
                ? (BoardExportStatusDto)reExportStatus
                : BoardExportStatusDto.None,
            document.ReExportOptions,
            document.ErrorMessage);
}
