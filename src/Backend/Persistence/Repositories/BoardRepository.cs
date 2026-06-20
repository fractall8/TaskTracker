using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BoardRepository(TaskTrackerDbContext dbContext) : Repository<Board, Guid>(dbContext), IBoardRepository
{
    public async Task<Board?> GetBoardWithHierarchyAsync(Guid boardId, string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Board> query;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();

            query = DbContext.Set<Board>()
                .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tasks
                    .Where(t =>
                        t.Title.ToLower().Contains(lowerSearchTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(lowerSearchTerm)))
                    .OrderBy(t => t.Position))
                .ThenInclude(t => t.Assignee)
                .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
                .ThenInclude(t => t.Reporter);
        }
        else
        {
            query = DbContext.Set<Board>()
                .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.Position))
                .ThenInclude(t => t.Assignee)
                .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
                .ThenInclude(t => t.Reporter);
        }

        return await query.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);
    }

    public async Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbContext.Boards
            .AsNoTracking()
            .Include(b => b.Members.Where(m => m.UserId == userId))
            .Where(b => b.Members.Any(m => m.UserId == userId))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> CountUserBoardsAsync(Guid userId, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbContext.Boards.Where(b => b.Members.Any(m => m.UserId == userId));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{searchTerm}%") ||
                                     EF.Functions.ILike(b.Description ?? string.Empty, $"%{searchTerm}%"));
        }

        return await query.CountAsync(ct);
    }

    public async Task<List<Board>> GetUserBoardsPaginatedAsync(Guid userId, int pageNumber, int pageSize,
        string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbContext.Boards
            .AsNoTracking()
            .Include(b => b.Members.Where(m => m.UserId == userId))
            .Where(b => b.Members.Any(m => m.UserId == userId));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{searchTerm}%") ||
                                     EF.Functions.ILike(b.Description ?? string.Empty, $"%{searchTerm}%"));
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        return await DbContext.Set<BoardMember>()
            .Where(m => m.BoardId == boardId && m.UserId == userId)
            .Select(m => (BoardRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }
}
