using Contracts.DTOs;

namespace Services.Abstractions.BoardCalls;

public interface IBoardCallApiService
{
    Task<BoardCallDto?> GetActiveCallAsync(Guid boardId, CancellationToken ct = default);

    Task<StartOrJoinBoardCallResponse> StartCallAsync(Guid boardId, CancellationToken ct = default);

    Task<StartOrJoinBoardCallResponse> JoinCallAsync(Guid boardId, CancellationToken ct = default);

    Task LeaveCallAsync(Guid boardId, CancellationToken ct = default);

    Task EndCallAsync(Guid boardId, CancellationToken ct = default);
}
