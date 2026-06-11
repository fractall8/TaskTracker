using Contracts.DTOs;
using Refit;

namespace Services.Abstractions.Auth;

public interface IAuthApiService
{
    Task<UserWithRoleDto> LoginAsync(CancellationToken ct = default);
    
    Task<UserWithRoleDto?> GetCurrentUserAsync(CancellationToken ct = default);
}