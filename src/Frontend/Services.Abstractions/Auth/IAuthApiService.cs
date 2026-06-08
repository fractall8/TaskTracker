using Contracts.DTOs;
using Refit;

namespace Services.Abstractions.Auth;

public interface IAuthApiService
{
    Task<UserWithRolesDto> LoginAsync(CancellationToken ct = default);
    
    Task<UserWithRolesDto?> GetCurrentUserAsync(CancellationToken ct = default);
}