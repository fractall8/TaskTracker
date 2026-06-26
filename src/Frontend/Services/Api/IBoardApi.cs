using Contracts.DTOs;
using Contracts.Requests.Boards;
using Refit;

namespace Services.Api;

public interface IBoardApi
{
    [Get("/api/boards")]
    Task<IApiResponse<PagedList<BoardPreviewDto>>> GetBoardsAsync(
        [Query] int pageNumber,
        [Query] int pageSize,
        [Query] string? searchTerm = null,
        CancellationToken ct = default);

    [Post("/api/workspaces/{workspaceId}/boards")]
    Task<IApiResponse<BoardPreviewDto>> CreateBoardAsync(
        Guid workspaceId,
        [Body] CreateBoardRequest request,
        CancellationToken ct = default);

    [Put("/api/boards/{id}")]
    Task<IApiResponse<BoardPreviewDto>> UpdateBoardAsync(
        Guid id,
        [Body] UpdateBoardRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{id}")]
    Task<IApiResponse> DeleteBoardAsync(Guid id, CancellationToken ct = default);

    [Get("/api/boards/{id}")]
    Task<IApiResponse<BoardWithColumnsDto>> GetByIdAsync(Guid id, [Query] string? searchTerm = null, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/leave")]
    Task<IApiResponse> LeaveBoardAsync(Guid boardId, CancellationToken ct = default);
}
