using Application.Interfaces;
using Contracts.DTOs;
using Contracts.Enums;
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
        var query = DbContext.Boards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();

            query = query
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
            query = query
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
            .Include(b => b.Members.Where(m => m.WorkspaceMember!.UserId == userId))
            .Where(b => b.Members.Any(m => m.WorkspaceMember!.UserId == userId))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> CountUserBoardsAsync(Guid userId, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbContext.Boards
            .Where(b => b.Members.Any(m => m.WorkspaceMember!.UserId == userId));

        return await ApplySearchFilter(query, searchTerm).CountAsync(ct);
    }

    public async Task<List<Board>> GetUserBoardsPaginatedAsync(Guid userId, int pageNumber, int pageSize,
        string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbContext.Boards
            .AsNoTracking()
            .Include(b => b.Members.Where(m => m.WorkspaceMember!.UserId == userId))
            .Where(b => b.Members.Any(m => m.WorkspaceMember!.UserId == userId));

        return await ApplySearchFilter(query, searchTerm)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        return await DbContext.Set<BoardMember>()
            .Where(m => m.BoardId == boardId && m.WorkspaceMember!.UserId == userId)
            .Select(m => (BoardRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Board>> GetBoardsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(b => b.WorkspaceId == workspaceId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> CountBoardsByWorkspaceIdAsync(Guid workspaceId, Guid userId, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbSet
            .Where(b => b.WorkspaceId == workspaceId && b.Members.Any(m => m.WorkspaceMember!.UserId == userId));

        return await ApplySearchFilter(query, searchTerm).CountAsync(ct);
    }

    public async Task<List<Board>> GetBoardsByWorkspaceIdPaginatedAsync(Guid workspaceId, Guid userId, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(b => b.Members.Where(m => m.WorkspaceMember!.UserId == userId))
            .Where(b => b.WorkspaceId == workspaceId && b.Members.Any(m => m.WorkspaceMember!.UserId == userId));

        return await ApplySearchFilter(query, searchTerm)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<BoardMemberDto>> GetBoardMembersAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Set<BoardMember>()
            .AsNoTracking()
            .Where(bm => bm.BoardId == boardId)
            .Select(bm => new BoardMemberDto(
                bm.WorkspaceMemberId,
                bm.WorkspaceMember!.User!.Email,
                bm.WorkspaceMember.User.DisplayName,
                bm.WorkspaceMember.User.AvatarUrl,
                (BoardRoleDto)bm.Role,
                bm.JoinedAt
            ))
            .ToListAsync(ct);
    }

    private static IQueryable<Board> ApplySearchFilter(IQueryable<Board> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        return query.Where(b => EF.Functions.ILike(b.Name, $"%{searchTerm}%") ||
                                EF.Functions.ILike(b.Description ?? string.Empty, $"%{searchTerm}%"));
    }
}
