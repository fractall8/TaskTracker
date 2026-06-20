using Contracts.DTOs;
using Contracts.Requests;

namespace Services.Abstractions.Boards;

public interface IBoardApiService
{
    Task<PagedList<BoardPreviewDto>> GetMyBoardsAsync(int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<BoardWithColumnsDto>
        GetBoardByIdAsync(Guid boardId, string? searchTerm = null, CancellationToken ct = default);

    Task<BoardPreviewDto?> CreateBoardAsync(UpdateBoardRequest request, CancellationToken ct = default);

    Task<BoardPreviewDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken ct = default);

    Task DeleteBoardAsync(Guid id, CancellationToken ct = default);
}
