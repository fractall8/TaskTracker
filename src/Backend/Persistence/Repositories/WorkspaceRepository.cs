using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class WorkspaceRepository(TaskTrackerDbContext dbContext)
    : Repository<Workspace, Guid>(dbContext), IWorkspaceRepository
{
    public async Task<bool> ExistsAsync(Guid workspaceId, CancellationToken ct = default) =>
        await DbSet.AnyAsync(w => w.Id == workspaceId, ct);

    public async Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct = default) =>
        await DbContext.Set<WorkspaceMember>()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Workspace>> GetUserWorkspacesAsync(Guid userId, CancellationToken ct = default) =>
        await DbContext.Set<WorkspaceMember>()
            .Where(m => m.UserId == userId)
            .Include(m => m.Workspace)
            .Select(m => m.Workspace!)
            .ToListAsync(ct);

    public async Task<List<(Workspace Workspace, WorkspaceRole Role)>> GetUserWorkspacesWithRolesAsync(Guid userId,
        CancellationToken ct = default)
    {
        var memberships = await DbContext.Set<WorkspaceMember>()
            .Where(m => m.UserId == userId)
            .Include(m => m.Workspace)
            .Select(m => new { m.Workspace, m.Role })
            .ToListAsync(ct);

        return memberships
            .Where(m => m.Workspace != null)
            .Select(m => (m.Workspace!, m.Role))
            .ToList();
    }

    public async Task<Workspace?> GetByIdWithMembersAsync(Guid workspaceId, CancellationToken ct = default) =>
        await DbSet
            .Include(w => w.Members.OrderBy(m => m.JoinedAt).ThenBy(m => m.Id))
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

    public async Task SoftDeleteCascadeAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        await DbContext.Comments
            .Where(c => c.Task!.Column!.Board!.WorkspaceId == workspaceId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true).SetProperty(c => c.DeletedAt, now), ct);

        await DbContext.Attachments
            .Where(a => a.Task!.Column!.Board!.WorkspaceId == workspaceId && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true).SetProperty(a => a.DeletedAt, now), ct);

        await DbContext.Tasks
            .Where(t => t.Column!.Board!.WorkspaceId == workspaceId && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true).SetProperty(t => t.DeletedAt, now), ct);

        await DbContext.Columns
            .Where(c => c.Board!.WorkspaceId == workspaceId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true).SetProperty(c => c.DeletedAt, now), ct);

        await DbContext.BoardMembers
            .Where(bm => bm.Board!.WorkspaceId == workspaceId && !bm.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(bm => bm.IsDeleted, true).SetProperty(bm => bm.DeletedAt, now), ct);

        await DbContext.Boards
            .Where(b => b.WorkspaceId == workspaceId && !b.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, true).SetProperty(b => b.DeletedAt, now), ct);

        await DbContext.WorkspaceInvites
            .Where(wi => wi.WorkspaceId == workspaceId && !wi.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(wi => wi.IsDeleted, true).SetProperty(wi => wi.DeletedAt, now), ct);

        await DbContext.WorkspaceMembers
            .Where(wm => wm.WorkspaceId == workspaceId && !wm.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(wm => wm.IsDeleted, true).SetProperty(wm => wm.DeletedAt, now), ct);

        await DbContext.Workspaces
            .Where(w => w.Id == workspaceId && !w.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.IsDeleted, true).SetProperty(w => w.DeletedAt, now), ct);
    }
}
