using Contracts.DTOs;
using Refit;

namespace Services.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<IApiResponse<UserWithRolesDto>> LoginAsync(CancellationToken ct = default);

    [Get("/api/auth/me")]
    Task<IApiResponse<UserWithRolesDto>> GetCurrentUserAsync(CancellationToken ct = default);
}