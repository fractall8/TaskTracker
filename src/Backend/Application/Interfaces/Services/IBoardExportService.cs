using Contracts.DTOs;

namespace Application.Interfaces.Services;

public interface IBoardExportService
{
    IAsyncEnumerable<BoardExportStatusInfoDto> ScanForRequestedExportStatusesAsync(
        int maxDocuments,
        CancellationToken ct = default);

    IAsyncEnumerable<BoardExportStatusInfoDto> ScanForFailedExportStatusesAsync(
        int maxDocuments,
        DateTime failedCooldownThreshold,
        CancellationToken ct = default);

    IAsyncEnumerable<BoardExportStatusInfoDto> ScanForStaleExportStatusesAsync(
        int maxDocuments,
        DateTime staleCooldownThreshold,
        CancellationToken ct = default);

    Task UpdateReExportStatusAsync(Guid boardId, BoardExportStatusDto reExportStatus, string? errorMessage = null,
        CancellationToken ct = default);

    Task SetExportAsync(Guid boardId, BoardExportStatusDto exportStatus, BoardExportOptionsDto exportOptions,
        CancellationToken ct = default);

    Task UpdateExportStatusAsync(Guid boardId, BoardExportStatusDto status, string? errorMessage = null,
        CancellationToken ct = default);

    Task SetReExportAsync(Guid boardId, BoardExportStatusDto reExportStatus, BoardExportOptionsDto reExportOptions,
        CancellationToken ct = default);

    Task<BoardExportStatusInfoDto?> GetBoardExportInfoAsync(Guid boardId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, BoardExportStatusInfoDto>> GetBoardListExportInfoAsync(
        IReadOnlyCollection<Guid> boardIds, CancellationToken ct = default);
}
