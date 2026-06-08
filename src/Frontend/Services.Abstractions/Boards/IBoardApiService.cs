using Contracts.DTOs;

namespace Services.Abstractions.Boards;

public interface IBoardApiService
{
    Task<List<BoardPreviewDto>> GetMyBoardsAsync();
    
    Task<BoardPreviewDto?> CreateBoardAsync(UpdateBoardRequest request);
    
    Task<BoardPreviewDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request);
    
    Task DeleteBoardAsync(Guid id);
}