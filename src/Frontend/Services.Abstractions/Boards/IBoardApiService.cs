using Contracts.DTOs;
using Contracts.Requests.Boards;

namespace Services.Abstractions.Boards;

public interface IBoardApiService
{
    Task<PagedList<BoardPreviewDto>> GetMyBoardsAsync(int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<BoardWithColumnsDto>
        GetBoardByIdAsync(Guid boardId, string? searchTerm = null, CancellationToken ct = default);

    Task<BoardPreviewDto?> CreateBoardAsync(Guid workspaceId, CreateBoardRequest request, CancellationToken ct = default);

    Task<BoardPreviewDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken ct = default);

    Task DeleteBoardAsync(Guid id, CancellationToken ct = default);

    Task LeaveBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardArchiveDownloadDto> GetBoardArchiveDownloadUrlAsync(Guid boardId, CancellationToken ct = default);

    Task ArchiveAndExportBoardAsync(Guid boardId, BoardExportOptionsDto exportOptions, CancellationToken ct = default);

    Task ReExportBoardAsync(Guid boardId, BoardExportOptionsDto options, CancellationToken ct = default);
}
