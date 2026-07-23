namespace Application.Interfaces.UOW;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);

    Task AcquireDistributedLockAsync(string lockKey, CancellationToken cancellationToken = default);
}
