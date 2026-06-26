using Application.Interfaces.UOW;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class UnitOfWork(TaskTrackerDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
