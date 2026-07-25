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

    public async Task<int> CountActiveParticipantsAsync(Guid boardCallId, CancellationToken ct = default)
    {
        return await DbContext.BoardCallParticipants
            .CountAsync(p => p.BoardCallId == boardCallId && p.LeftAt == null, ct);
    }
}
