using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class WorkspaceInviteRepository(TaskTrackerDbContext dbContext) : Repository<WorkspaceInvite, Guid>(dbContext), IWorkspaceInviteRepository
{
    public async Task<WorkspaceInvite?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(i => i.Token == token, ct);

    public async Task<List<WorkspaceInvite>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
