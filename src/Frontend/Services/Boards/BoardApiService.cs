using Contracts.DTOs;
using Contracts.Requests;
using Services.Abstractions.Boards;
using Services.Api;
using Services.Extensions;

namespace Services.Boards;

public class BoardApiService(IBoardApi boardApi) : IBoardApiService
{
    public async Task<PagedList<BoardPreviewDto>> GetMyBoardsAsync(int pageNumber, int pageSize, string? searchTerm, CancellationToken ct = default)
    {
        var response = await boardApi.GetBoardsAsync(pageNumber, pageSize, searchTerm, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<BoardWithColumnsDto> GetBoardByIdAsync(Guid boardId, string? searchTerm = null, CancellationToken ct = default)
    {
        var response = await boardApi.GetByIdAsync(boardId, searchTerm, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<BoardPreviewDto?> CreateBoardAsync(UpdateBoardRequest request, CancellationToken ct = default)
    {
        var response = await boardApi.CreateBoardAsync(request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<BoardPreviewDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, CancellationToken ct = default)
    {
        var response = await boardApi.UpdateBoardAsync(id, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task DeleteBoardAsync(Guid id, CancellationToken ct = default)
    {
        var response = await boardApi.DeleteBoardAsync(id, ct);
        await response.HandleResponseAsync();
    }
}