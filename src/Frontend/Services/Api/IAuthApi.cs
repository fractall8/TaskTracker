using Contracts.DTOs;
using Refit;

namespace Services.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<IApiResponse<UserWithRoleDto>> LoginAsync(CancellationToken ct = default);

    [Get("/api/auth/me")]
    Task<IApiResponse<UserWithRoleDto>> GetCurrentUserAsync(CancellationToken ct = default);
}