using Contracts.DTOs;
using Services.Abstractions.Boards;
using Services.Api;

namespace Services.Boards;

public class BoardApiService(IBoardApi boardApi) : IBoardApiService
{
    public async Task<List<BoardPreviewDto>> GetMyBoardsAsync()
    {
        return await boardApi.GetBoardsAsync();
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