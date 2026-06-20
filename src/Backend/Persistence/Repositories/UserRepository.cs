using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class UserRepository(TaskTrackerDbContext context) : Repository<User, Guid>(context), IUserRepository
{
    public async Task<TProjection?> GetUserByAzureAdIdAsync<TProjection>(
        Guid azureAdObjectId,
        Expression<Func<User, TProjection>> selector,
        CancellationToken ct = default) =>
        await DbSet
            .Where(u => u.AzureAdObjectId == azureAdObjectId)
            .Select(selector)
            .FirstOrDefaultAsync(ct);

    public async Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        if (ids == null || !ids.Any())
        {
            return new List<User>();
        }

        return await DbContext.Set<User>()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);
    }
}
