using Contracts.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<UserDto?> GetUserDtoByAzureAdIdAsync(Guid azureAdObjectId, CancellationToken ct = default);

    Task<User?> GetUserByAzureAdIdAsync(Guid azureAdObjectId, CancellationToken ct = default);
}