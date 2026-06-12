using Contracts.DTOs;
using Contracts.Requests;
using Services.Abstractions.Boards;
using Services.Api;

namespace Services.Boards;

public class BoardApiService(IBoardApi boardApi) : IBoardApiService
{
    public async Task<PagedList<BoardPreviewDto>> GetMyBoardsAsync(int pageNumber, int pageSize, string? searchTerm, CancellationToken ct = default)
    {
        var response = await boardApi.GetBoardsAsync(pageNumber, pageSize, searchTerm, ct);
        
        if (!response.IsSuccessStatusCode || response.Content == null)
            throw new Exception($"Failed to get boards: {response.Error?.Message}");
            
        return response.Content;
    }

    public async Task<BoardWithColumnsDto> GetBoardByIdAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardApi.GetByIdAsync(boardId, ct);
        
        if (!response.IsSuccessStatusCode || response.Content == null)
            throw new Exception($"Failed to load board: {response.Error?.Message}");
            
        return response.Content;
    }

    public async Task<BoardPreviewDto> CreateBoardAsync(UpdateBoardRequest request, CancellationToken ct = default)
    {
        var response = await boardApi.CreateBoardAsync(request, ct);
        
        if (!response.IsSuccessStatusCode || response.Content == null)
            throw new Exception($"Failed to create board: {response.Error?.Message}");
            
        return response.Content;
    }

    public async Task<BoardPreviewDto> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken ct = default)
    {
        var response = await boardApi.UpdateBoardAsync(id, request, ct);
        
        if (!response.IsSuccessStatusCode || response.Content == null)
            throw new Exception($"Failed to update board: {response.Error?.Message}");
            
        return response.Content;
    }

    public async Task DeleteBoardAsync(Guid id, CancellationToken ct = default)
    {
        var response = await boardApi.DeleteBoardAsync(id, ct);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to delete board: {response.Error?.Message}");
    }
}