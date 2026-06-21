using System.Linq.Expressions;
using Contracts.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<TProjection?> GetUserByAzureAdIdAsync<TProjection>(
        Guid azureAdObjectId,
        Expression<Func<User, TProjection>> selector,
        CancellationToken ct = default);

    Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<List<UserDto>> SearchWorkspaceUsersAsync(Guid workspaceId, string? searchTerm, CancellationToken ct = default);
}