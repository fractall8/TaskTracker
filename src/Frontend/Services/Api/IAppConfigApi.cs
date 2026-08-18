using Contracts.DTOs;
using Refit;

namespace Services.Api;

public interface IAppConfigApi
{
    [Get("/api/config")]
    Task<IApiResponse<AppConfigDto>> GetAsync(CancellationToken ct = default);
}
