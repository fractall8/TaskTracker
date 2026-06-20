using Contracts.DTOs;
using Refit;

namespace Services.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<IApiResponse<UserDto>> LoginAsync(CancellationToken ct = default);

    [Get("/api/auth/me")]
    Task<IApiResponse<UserDto>> GetCurrentUserAsync(CancellationToken ct = default);
}
