using Contracts.DTOs;
using Contracts.Requests;
using Refit;

namespace Services.Api;

public interface IBoardApi
{
    [Get("/api/boards")]
    Task<PagedList<BoardPreviewDto>> GetBoardsAsync(
        [Query] int pageNumber, 
        [Query] int pageSize, 
        [Query] string? searchTerm = null,
        CancellationToken ct = default);

    [Post("/api/boards")]
    Task<BoardPreviewDto> CreateBoardAsync([Body] UpdateBoardRequest request);

    [Put("/api/boards/{id}")]
    Task<BoardPreviewDto> UpdateBoardAsync(Guid id, [Body] UpdateBoardRequest request);

    [Delete("/api/boards/{id}")]
    Task DeleteBoardAsync(Guid id);
}