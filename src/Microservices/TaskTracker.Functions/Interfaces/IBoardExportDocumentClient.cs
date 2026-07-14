
using Contracts.DTOs;

namespace TaskTracker.Functions.Interfaces;
public interface IBoardExportDocumentClient
{
    Task<BoardExportStatusInfoDto?> GetBoardExportInfoAsync(Guid boardId, CancellationToken ct = default);

    Task MarkExportProcessingAsync(Guid boardId, CancellationToken ct = default);

    Task CompleteInitialExportAsync(Guid boardId, CancellationToken ct = default);

    Task FailInitialExportAsync(Guid boardId, string errorMessage, CancellationToken ct = default);

    Task MarkReExportProcessingAsync(Guid boardId, CancellationToken ct = default);

    Task CompleteReExportAsync(Guid boardId, BoardExportOptionsDto promotedExportOptions, CancellationToken ct = default);

    Task FailReExportAsync(Guid boardId, string errorMessage, CancellationToken ct = default);
}
