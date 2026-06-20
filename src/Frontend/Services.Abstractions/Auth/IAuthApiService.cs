using Contracts.DTOs;

namespace Services.Abstractions.Auth;

public interface IAuthApiService
{
    Task<UserDto> LoginAsync(CancellationToken ct = default);

    Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default);
}
