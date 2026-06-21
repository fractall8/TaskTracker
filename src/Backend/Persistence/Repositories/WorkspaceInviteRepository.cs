using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class WorkspaceInviteRepository(TaskTrackerDbContext dbContext) : Repository<WorkspaceInvite, Guid>(dbContext), IWorkspaceInviteRepository
{
    public async Task<WorkspaceInvite?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(i => i.Token == token, ct);
}
