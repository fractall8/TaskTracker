using Contracts.DTOs;
using Contracts.Requests;
using Services.Abstractions.Boards;
using Services.Api;

namespace Services.Boards;

public class BoardApiService(IBoardApi boardApi) : IBoardApiService
{
    public async Task<PagedList<BoardPreviewDto>> GetMyBoardsAsync(int pageNumber, int pageSize, string? searchTerm)
    {
        return await boardApi.GetBoardsAsync(pageNumber, pageSize, searchTerm);
    }
    
    public async Task<BoardPreviewDto?> CreateBoardAsync(UpdateBoardRequest request)
    {
        return await boardApi.CreateBoardAsync(request);
    }

    public async Task<BoardPreviewDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request)
    {
        return await boardApi.UpdateBoardAsync(id, request);
    }

    public async Task DeleteBoardAsync(Guid id)
    {
        await boardApi.DeleteBoardAsync(id);
    }
}