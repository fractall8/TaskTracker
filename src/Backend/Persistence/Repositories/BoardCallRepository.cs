using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BoardCallRepository(TaskTrackerDbContext dbContext) : Repository<BoardCall, Guid>(dbContext), IBoardCallRepository
{
    public async Task<BoardCall?> GetActiveCallForBoardAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.BoardId == boardId && c.EndedAt == null, ct);
    }

    public async Task<List<BoardCall>> GetActiveCallsForWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(c => c.EndedAt == null && c.Board!.WorkspaceId == workspaceId)
            .ToListAsync(ct);
    }

    public async Task<BoardCall?> GetActiveCallByAcsRoomIdAsync(string acsRoomId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.AcsRoomId == acsRoomId && c.EndedAt == null, ct);
    }

    public async Task<BoardCall?> GetActiveCallWithParticipantsAsync(Guid boardCallId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(c => c.Participants.Where(p => p.LeftAt == null))
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == boardCallId && c.EndedAt == null, ct);
    }

    public async Task<BoardCallParticipant?> GetActiveParticipantAsync(Guid boardCallId, Guid userId, CancellationToken ct = default)
    {
        return await DbContext.BoardCallParticipants
            .FirstOrDefaultAsync(p => p.BoardCallId == boardCallId && p.UserId == userId && p.LeftAt == null, ct);
    }

    public async Task<BoardCallParticipant?> GetLatestParticipantAsync(Guid boardCallId, Guid userId, CancellationToken ct = default)
    {
        return await DbContext.BoardCallParticipants
            .Where(p => p.BoardCallId == boardCallId && p.UserId == userId)
            .OrderByDescending(p => p.JoinedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountActiveParticipantsAsync(Guid boardCallId, CancellationToken ct = default)
    {
        return await DbContext.BoardCallParticipants
            .CountAsync(p => p.BoardCallId == boardCallId && p.LeftAt == null, ct);
    }

    public async Task AddParticipantAsync(BoardCallParticipant participant, CancellationToken ct = default)
    {
        await DbContext.BoardCallParticipants.AddAsync(participant, ct);
    }

    public void UpdateParticipant(BoardCallParticipant participant)
    {
        DbContext.BoardCallParticipants.Update(participant);
    }

    public void DeleteParticipant(BoardCallParticipant participant)
    {
        DbContext.BoardCallParticipants.Remove(participant);
    }
}
