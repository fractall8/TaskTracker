using Contracts.DTOs;
using Contracts.Requests.Boards;
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

    public async Task<BoardPreviewDto?> CreateBoardAsync(Guid workspaceId, CreateBoardRequest request, CancellationToken ct = default)
    {
        var response = await boardApi.CreateBoardAsync(workspaceId, request, ct);
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

    public async Task LeaveBoardAsync(Guid boardId, CancellationToken ct = default) =>
        await (await boardApi.LeaveBoardAsync(boardId, ct)).HandleResponseAsync();

    public async Task<BoardArchiveDownloadDto> GetBoardArchiveDownloadUrlAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardApi.GetArchiveDownloadUrlAsync(boardId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task ArchiveAndExportBoardAsync(Guid boardId, BoardExportOptionsDto exportOptions, CancellationToken ct = default)
    {
        var response = await boardApi.ArchiveAndExportAsync(boardId, exportOptions, ct);
        await response.HandleResponseAsync();
    }

    public async Task ReExportBoardAsync(Guid boardId, BoardExportOptionsDto options, CancellationToken ct = default) =>
        await (await boardApi.ReExportBoardAsync(boardId, options, ct)).HandleResponseAsync();
}
