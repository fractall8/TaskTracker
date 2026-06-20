using Contracts.DTOs;
using Contracts.Requests;
using Refit;

namespace Services.Api;

public interface IColumnsApi
{
    [Post("/api/boards/{boardId}/columns")]
    Task<IApiResponse<ColumnDto>> CreateAsync(
        Guid boardId,
        [Body] CreateColumnRequest request,
        CancellationToken ct = default);

    [Put("/api/boards/{boardId}/columns/{columnId}")]
    Task<IApiResponse<ColumnDto>> UpdateAsync(
        Guid boardId,
        Guid columnId,
        [Body] UpdateColumnRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/columns/{columnId}")]
    Task<IApiResponse> DeleteAsync(
        Guid boardId,
        Guid columnId,
        CancellationToken ct = default);

    [Put("/api/boards/{boardId}/columns/{columnId}/move")]
    Task<IApiResponse> MoveAsync(
        Guid boardId,
        Guid columnId,
        [Body] int newPosition,
        CancellationToken ct = default);
}
