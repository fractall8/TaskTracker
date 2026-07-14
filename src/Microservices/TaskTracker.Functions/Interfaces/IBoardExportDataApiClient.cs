using Contracts.DTOs;

namespace TaskTracker.Functions.Interfaces;

public interface IBoardExportDataApiClient
{
    Task<BoardExportDataDto> GetExportDataAsync(Guid boardId, BoardExportOptionsDto exportOptions,
        CancellationToken ct = default);
}
