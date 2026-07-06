using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class ColumnRepository(TaskTrackerDbContext dbContext)
    : Repository<Column, Guid>(dbContext), IColumnRepository
{
    public async Task<IEnumerable<string>> GetNameListByBoardIdAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Columns.Where(c => c.BoardId == boardId && !c.IsDeleted).Select(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task DecrementPositionsAsync(Guid boardId, int startingFromPosition, CancellationToken ct)
    {
        await DbContext.Columns
            .Where(c => c.BoardId == boardId && c.Position > startingFromPosition)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1), ct);
    }


    public async Task UpdatePositionsOnMoveAsync(Guid boardId, int oldPosition, int newPosition, CancellationToken ct)
    {
        if (oldPosition < newPosition)
        {
            await DbContext.Columns
                .Where(c => c.BoardId == boardId && c.Position > oldPosition && c.Position <= newPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1), ct);
        }
        else if (oldPosition > newPosition)
        {
            await DbContext.Columns
                .Where(c => c.BoardId == boardId && c.Position >= newPosition && c.Position < oldPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position + 1), ct);
        }
    }

    public async Task<int> GetMaxPositionAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Columns
            .Where(c => c.BoardId == boardId && !c.IsDeleted)
            .Select(c => (int?)c.Position)
            .MaxAsync(ct) ?? 0;
    }

    public async Task SoftDeleteCascadeAsync(Guid columnId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        await DbContext.Comments
            .Where(c => c.Task!.ColumnId == columnId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true).SetProperty(c => c.DeletedAt, now), ct);

        await DbContext.Attachments
            .Where(a => a.Task!.ColumnId == columnId && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true).SetProperty(a => a.DeletedAt, now), ct);

        await DbContext.Tasks
            .Where(t => t.ColumnId == columnId && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true).SetProperty(t => t.DeletedAt, now), ct);

        await DbContext.Columns
            .Where(c => c.Id == columnId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true).SetProperty(c => c.DeletedAt, now), ct);
    }
}
