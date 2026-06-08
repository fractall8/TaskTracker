using Contracts.DTOs;
using Refit;

namespace Services.Api;

public interface IBoardApi
{
    [Get("/api/boards")]
    Task<List<BoardPreviewDto>> GetBoardsAsync();

    [Post("/api/boards")]
    Task<BoardPreviewDto> CreateBoardAsync([Body] UpdateBoardRequest request);

    [Put("/api/boards/{id}")]
    Task<BoardPreviewDto> UpdateBoardAsync(Guid id, [Body] UpdateBoardRequest request);

    [Delete("/api/boards/{id}")]
    Task DeleteBoardAsync(Guid id);
}