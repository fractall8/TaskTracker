using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class WorkspaceMemberRepository(TaskTrackerDbContext dbContext) : Repository<WorkspaceMember, Guid>(dbContext), IWorkspaceMemberRepository
{
    public async Task<WorkspaceMember?> GetByWorkspaceAndUserIdAsync(Guid workspaceId, Guid userId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
}
