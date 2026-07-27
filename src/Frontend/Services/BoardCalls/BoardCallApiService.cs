using Contracts.DTOs;
using Refit;
using Services.Abstractions.BoardCalls;
using Services.Api;
using Services.Extensions;

namespace Services.BoardCalls;

public class BoardCallApiService(IBoardCallsApi boardCallsApi) : IBoardCallApiService
{
    public async Task<BoardCallDto?> GetActiveCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallsApi.GetActiveAsync(boardId, ct);

        // HandleResponseAsync<T>() treats null Content as a failure, which doesn't fit this endpoint's
        // legitimately-nullable "no active call" response — use the non-generic overload for the
        // success/error check instead, and return Content (possibly null) directly.
        await ((IApiResponse)response).HandleResponseAsync();
        return response.Content;
    }

    public async Task<StartOrJoinBoardCallResponse> StartCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallsApi.StartAsync(boardId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<StartOrJoinBoardCallResponse> JoinCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallsApi.JoinAsync(boardId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task LeaveCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallsApi.LeaveAsync(boardId, ct);
        await response.HandleResponseAsync();
    }

    public async Task EndCallAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await boardCallsApi.EndAsync(boardId, ct);
        await response.HandleResponseAsync();
    }
}
