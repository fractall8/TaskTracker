using Contracts.DTOs;
using Contracts.Export;
using TaskTracker.Functions.Interfaces;

namespace TaskTracker.Functions.Processing.Export;

public sealed class InitialExportCompletionHandler(
    IBoardExportDocumentClient cosmos,
    IBoardExportStatusNotifyApiClient statusNotifyApiClient)
    : IBoardExportCompletionHandler
{
    public BoardExportType Type => BoardExportType.InitialExport;

    public async Task MarkProcessingAsync(BoardExportContext context, CancellationToken ct = default)
    {
        await cosmos.MarkExportProcessingAsync(context.BoardId, ct);
        await statusNotifyApiClient.NotifyExportStatusChangedAsync(
            context.BoardId,
            BoardExportStatusDto.Processing,
            ct);
    }

    public async Task MarkCompletedAsync(BoardExportContext context, CancellationToken ct = default)
    {
        await cosmos.CompleteInitialExportAsync(context.BoardId, ct);
        await statusNotifyApiClient.NotifyExportStatusChangedAsync(
            context.BoardId,
            BoardExportStatusDto.Completed,
            ct);
    }

    public async Task MarkFailedAsync(BoardExportContext context, string errorMessage, CancellationToken ct = default)
    {
        await cosmos.FailInitialExportAsync(context.BoardId, errorMessage, ct);
        await statusNotifyApiClient.NotifyExportStatusChangedAsync(
            context.BoardId,
            BoardExportStatusDto.Failed,
            ct);
    }
}
