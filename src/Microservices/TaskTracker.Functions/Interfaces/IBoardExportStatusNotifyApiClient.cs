
using Contracts.DTOs;

namespace TaskTracker.Functions.Interfaces;
public interface IBoardExportStatusNotifyApiClient
{
    Task NotifyExportStatusChangedAsync(
        Guid boardId,
        BoardExportStatusDto status,
        CancellationToken ct = default);

    Task NotifyReExportStatusChangedAsync(
        Guid boardId,
        BoardExportStatusDto status,
        BoardExportOptionsDto? exportOptions = null,
        CancellationToken ct = default);
}
