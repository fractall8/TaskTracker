using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Interfaces;

public interface IRepository<TEntity, TId> where TEntity : BaseEntity<TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);
}
