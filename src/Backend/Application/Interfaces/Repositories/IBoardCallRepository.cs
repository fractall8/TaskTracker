using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IBoardCallRepository : IRepository<BoardCall, Guid>
{
    Task<BoardCall?> GetActiveCallForBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardCall?> GetActiveCallByAcsRoomIdAsync(string acsRoomId, CancellationToken ct = default);

    Task<BoardCall?> GetActiveCallWithParticipantsAsync(Guid boardCallId, CancellationToken ct = default);

    Task<BoardCallParticipant?> GetActiveParticipantAsync(Guid boardCallId, Guid userId, CancellationToken ct = default);

    Task<int> CountActiveParticipantsAsync(Guid boardCallId, CancellationToken ct = default);

    Task AddParticipantAsync(BoardCallParticipant participant, CancellationToken ct = default);

    void UpdateParticipant(BoardCallParticipant participant);

    void DeleteParticipant(BoardCallParticipant participant);
}
