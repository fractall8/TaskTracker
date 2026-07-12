using Contracts.DTOs;
using Contracts.Export;
using TaskTracker.Functions.Interfaces;

namespace TaskTracker.Functions.Processing.Export;

public sealed class ReExportCompletionHandler(
    IBoardExportDocumentClient cosmos,
    IBoardExportStatusNotifyApiClient statusNotifyApiClient)
    : IBoardExportCompletionHandler
{
    public BoardExportType Type => BoardExportType.ReExport;

    public async Task MarkProcessingAsync(BoardExportContext context, CancellationToken ct = default)
    {
        await cosmos.MarkReExportProcessingAsync(context.BoardId, ct);
        await statusNotifyApiClient.NotifyReExportStatusChangedAsync(
            context.BoardId,
            BoardExportStatusDto.Processing,
            ct: ct);
    }

    public async Task MarkCompletedAsync(BoardExportContext context, CancellationToken ct = default)
    {
        if (context.Options is not { } promotedExportOptions)
        {
            throw new InvalidOperationException(
                $"Re-export options are missing for board {context.BoardId}.");
        }

        await cosmos.CompleteReExportAsync(context.BoardId, promotedExportOptions, ct);
        await statusNotifyApiClient.NotifyReExportStatusChangedAsync(
            context.BoardId,
            BoardExportStatusDto.None,
            promotedExportOptions,
            ct);
    }

    public async Task MarkFailedAsync(BoardExportContext context, string errorMessage, CancellationToken ct = default)
    {
        await cosmos.FailReExportAsync(context.BoardId, errorMessage, ct);
        await statusNotifyApiClient.NotifyReExportStatusChangedAsync(
            context.BoardId,
            BoardExportStatusDto.Failed,
            ct: ct);
    }
}
