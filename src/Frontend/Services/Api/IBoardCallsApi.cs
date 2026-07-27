using Contracts.DTOs;
using Refit;

namespace Services.Api;

public interface IBoardCallsApi
{
    [Get("/api/boards/{boardId}/calls/active")]
    Task<IApiResponse<BoardCallDto?>> GetActiveAsync(Guid boardId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/calls/start")]
    Task<IApiResponse<StartOrJoinBoardCallResponse>> StartAsync(Guid boardId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/calls/join")]
    Task<IApiResponse<StartOrJoinBoardCallResponse>> JoinAsync(Guid boardId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/calls/leave")]
    Task<IApiResponse> LeaveAsync(Guid boardId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/calls/end")]
    Task<IApiResponse> EndAsync(Guid boardId, CancellationToken ct = default);
}
