using Application.Interfaces;
using Contracts.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class UserRepository(TaskTrackerDbContext context) : Repository<User, Guid>(context), IUserRepository
{
    public async Task<UserDto?> GetUserDtoByAzureAdIdAsync(Guid azureAdObjectId, CancellationToken ct = default) =>
        await _dbSet
            .Where(u => u.AzureAdObjectId == azureAdObjectId)
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.DisplayName
            ))
            .FirstOrDefaultAsync(ct);

    public async Task<User?> GetUserByAzureAdIdAsync(Guid azureAdObjectId, CancellationToken ct = default) =>
        await _dbSet.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId, ct);
}