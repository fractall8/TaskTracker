using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class WorkspaceRepository(TaskTrackerDbContext dbContext) : Repository<Workspace, Guid>(dbContext), IWorkspaceRepository
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

    public async Task<List<(Workspace Workspace, WorkspaceRole Role)>> GetUserWorkspacesWithRolesAsync(Guid userId, CancellationToken ct = default)
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
            .Include(w => w.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
}
