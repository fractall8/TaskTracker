using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<TProjection?> GetUserByAzureAdIdAsync<TProjection>(
        Guid azureAdObjectId, 
        Expression<Func<User, TProjection>> selector, 
        CancellationToken ct = default);
    
    Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}