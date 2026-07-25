using System.Linq.Expressions;
using Application.Interfaces.Repositories;
using Contracts.DTOs;
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

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByAcsCommunicationUserIdAsync(string acsCommunicationUserId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.AcsCommunicationUserId == acsCommunicationUserId, ct);

    public async Task<List<UserDto>> SearchWorkspaceUsersAsync(Guid workspaceId, string? searchTerm, CancellationToken ct = default)
    {
        var query = DbContext.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId)
            .Select(m => m.User!);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lower = searchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(lower) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(lower)));
        }

        return await query
            .Select(u => new UserDto(u.Id, u.Email, u.DisplayName, u.AvatarUrl))
            .ToListAsync(ct);
    }
}
