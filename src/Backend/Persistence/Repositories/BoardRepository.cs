using Application.Interfaces.Repositories;
using Contracts.DTOs;
using Contracts.Enums;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class BoardRepository(TaskTrackerDbContext dbContext) : Repository<Board, Guid>(dbContext), IBoardRepository
{
    public async Task<int> CountMemberWorkspaceBoardsAsync(Guid workspaceId, Guid userId, string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(b =>
            b.WorkspaceId == workspaceId && b.Members.Any(m => m.WorkspaceMember!.UserId == userId));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{searchTerm}%"));
        }

        return await query.CountAsync(ct);
    }

    public async Task<int> CountArchivedMemberWorkspaceBoardsAsync(Guid workspaceId, Guid userId, string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(b =>
            b.WorkspaceId == workspaceId && b.Members.Any(m => m.WorkspaceMember!.UserId == userId) && b.IsArchived &&
            !b.IsDeleted).IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{searchTerm}%"));
        }

        return await query.CountAsync(ct);
    }

    public async Task<List<Board>> GetMemberWorkspaceBoardsPaginatedAsync(Guid workspaceId, Guid userId, int pageNumber,
        int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = DbSet.Where(b =>
            b.WorkspaceId == workspaceId && b.Members.Any(m => m.WorkspaceMember!.UserId == userId));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{searchTerm}%"));
        }

        return await query
            .Include(b => b.Members)
            .ThenInclude(m => m.WorkspaceMember)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<Board>> GetMyArchivedWorkspaceBoardsAsync(Guid workspaceId, Guid userId, int pageNumber,
        int pageSize, string? searchTerm,
        CancellationToken ct = default)
    {
        var query = DbContext.Boards
            .IgnoreQueryFilters()
            .Where(b => b.WorkspaceId == workspaceId && b.IsArchived && !b.IsDeleted);

        query = query.Where(b => b.Members.Any(m => m.WorkspaceMember!.UserId == userId));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => b.Name.Contains(searchTerm));
        }

        return await query
            .Include(b => b.Members)
            .ThenInclude(m => m.WorkspaceMember)
            .OrderByDescending(b => b.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<Board?> GetBoardWithHierarchyAsync(Guid boardId, string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Boards.IgnoreQueryFilters().Where(b => !b.IsDeleted).AsQueryable();

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
                .ThenInclude(c => c.Tasks
                    .Where(t =>
                        t.Title.ToLower().Contains(lowerSearchTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(lowerSearchTerm)))
                    .OrderBy(t => t.Position))
                .ThenInclude(t => t.Reporter);
        }
        else
        {
            query = query
                .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.Position))
                .ThenInclude(t => t.Assignee)
                .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks.OrderBy(t => t.Position))
                .ThenInclude(t => t.Reporter);
        }

        return await query.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);
    }

    public async Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        return await DbContext.Set<BoardMember>()
            .Where(m => m.BoardId == boardId && m.WorkspaceMember!.UserId == userId)
            .Select(m => (BoardRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<BoardMemberDto>> GetBoardMembersAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Set<BoardMember>()
            .AsNoTracking()
            .Where(bm => bm.BoardId == boardId)
            .Select(bm => new BoardMemberDto(
                bm.WorkspaceMemberId,
                bm.WorkspaceMember!.UserId,
                bm.WorkspaceMember!.User!.Email,
                bm.WorkspaceMember.User.DisplayName,
                bm.WorkspaceMember.User.AvatarUrl,
                (BoardRoleDto)bm.Role,
                (WorkspaceRoleDto)bm.WorkspaceMember.Role,
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

    public async Task SoftDeleteCascadeAsync(Guid boardId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        await DbContext.Comments
            .Where(c => c.Task!.Column!.BoardId == boardId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true).SetProperty(c => c.DeletedAt, now), ct);

        await DbContext.Attachments
            .Where(a => a.Task!.Column!.BoardId == boardId && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true).SetProperty(a => a.DeletedAt, now), ct);

        await DbContext.Tasks
            .Where(t => t.Column!.BoardId == boardId && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true).SetProperty(t => t.DeletedAt, now), ct);

        await DbContext.Columns
            .Where(c => c.BoardId == boardId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true).SetProperty(c => c.DeletedAt, now), ct);

        await DbContext.BoardMembers
            .Where(bm => bm.BoardId == boardId && !bm.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(bm => bm.IsDeleted, true).SetProperty(bm => bm.DeletedAt, now), ct);

        await DbContext.Boards
            .Where(b => b.Id == boardId && !b.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, true).SetProperty(b => b.DeletedAt, now), ct);
    }

    public async Task<BoardExportDataDto?> GetBoardExportDataAsync(Guid boardId, BoardExportOptionsDto options,
        CancellationToken ct = default)
    {
        var board = await DbContext.Boards
            .IgnoreQueryFilters()
            .Where(b => b.Id == boardId && b.IsArchived && !b.IsDeleted)
            .Select(b => new BoardExportBoardDto(
                b.Id,
                b.Name,
                b.CreatedAt,
                b.IsArchived,
                b.UpdatedAt,
                b.Columns.Count,
                b.Columns.SelectMany(c => c.Tasks).Count()))
            .FirstOrDefaultAsync(ct);

        if (board is null)
        {
            return null;
        }

        var columnData = await DbContext.Columns
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Position)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Position,
                Tasks = c.Tasks
                    .OrderBy(t => t.Position)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Position,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.Description,
                        Reporter = new BoardExportUserDto(
                            t.Reporter!.Id,
                            t.Reporter.Email,
                            t.Reporter.DisplayName),
                        Assignee = t.Assignee == null
                            ? null
                            : new BoardExportUserDto(
                                t.Assignee.Id,
                                t.Assignee.Email,
                                t.Assignee.DisplayName)
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        Dictionary<Guid, List<BoardExportCommentDto>> commentsByTask = [];

        if (options.IncludeComments)
        {
            var comments = await DbContext.Comments
                .Where(c => c.Task!.Column!.BoardId == boardId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.TaskId,
                    Comment = new BoardExportCommentDto(
                        c.Id,
                        c.Text,
                        c.CreatedAt,
                        c.UpdatedAt,
                        new BoardExportUserDto(c.AuthorId, c.Author!.Email, c.Author.DisplayName))
                })
                .ToListAsync(ct);

            commentsByTask = comments
                .GroupBy(x => x.TaskId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Comment).ToList());
        }

        Dictionary<Guid, List<BoardExportAttachmentDto>> attachmentsByTask = [];

        if (options.IncludeAttachments)
        {
            var attachments = await DbContext.Attachments
                .Where(a => a.Task!.Column!.BoardId == boardId)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new
                {
                    a.TaskId,
                    Attachment = new BoardExportAttachmentDto(
                        a.Id,
                        a.FileName,
                        a.ContentType ?? "application/octet-stream",
                        a.SizeInBytes,
                        a.CreatedAt,
                        new BoardExportUserDto(a.UploadedById, a.UploadedBy!.Email, a.UploadedBy.DisplayName),
                        Path.GetFileName(new Uri(a.FileUrl).LocalPath))
                })
                .ToListAsync(ct);

            attachmentsByTask = attachments
                .GroupBy(x => x.TaskId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Attachment).ToList());
        }

        List<BoardExportMemberDto>? members = null;

        if (options.IncludeMembers)
        {
            members = await DbContext.BoardMembers
                .Where(m => m.BoardId == boardId)
                .OrderBy(m => m.Role)
                .ThenBy(m => m.JoinedAt)
                .Select(m => new BoardExportMemberDto(
                    new BoardExportUserDto(
                        m.WorkspaceMember!.User!.Id,
                        m.WorkspaceMember.User.Email,
                        m.WorkspaceMember.User.DisplayName),
                    m.Role.ToString(),
                    m.JoinedAt))
                .ToListAsync(ct);
        }

        var rawColumns = columnData
            .Select(c => new BoardExportColumnDto(
                c.Id,
                c.Name,
                c.Position,
                c.Tasks
                    .Select(t => new BoardExportTaskDto(
                        t.Id,
                        t.Title,
                        t.Position,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.Reporter,
                        t.Assignee,
                        options.IncludeDescriptions ? t.Description : null,
                        commentsByTask.TryGetValue(t.Id, out var tc) ? tc : options.IncludeComments ? [] : null,
                        attachmentsByTask.TryGetValue(t.Id, out var ta) ? ta : options.IncludeAttachments ? [] : null))
                    .ToList()))
            .ToList();

        return new BoardExportDataDto(board, options, DateTimeOffset.UtcNow, rawColumns, members);
    }

    public async Task<bool> IsBoardArchivedAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Boards
            .IgnoreQueryFilters()
            .Where(b => b.Id == boardId)
            .Select(b => b.IsArchived)
            .FirstOrDefaultAsync(ct);
    }
}
