using Application.Interfaces;
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
}
