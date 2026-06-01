using Application.Interfaces;
using Persistence.Contexts;

namespace Persistence.UnitOfWork;

public class UnitOfWork(TaskTrackerDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}