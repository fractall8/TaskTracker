using Contracts.DTOs;
using Contracts.Requests;

namespace Services.Abstractions.Boards;

public interface IBoardApiService
{
    Task<PagedList<BoardPreviewDto>> GetMyBoardsAsync(int pageNumber, int pageSize, string? searchTerm = null);
    
    Task<BoardPreviewDto?> CreateBoardAsync(UpdateBoardRequest request);
    
    Task<BoardPreviewDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request);
    
    Task DeleteBoardAsync(Guid id);
}