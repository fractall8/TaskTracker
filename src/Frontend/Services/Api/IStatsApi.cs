using Contracts.DTOs;
using Contracts.Enums;
using Refit;

namespace Services.Api;

public interface IStatsApi
{
    [Get("/api/workspaces/{workspaceId}/stats")]
    Task<IApiResponse<WorkspaceStatsDto>> GetAsync(
        Guid workspaceId,
        StatsPeriodDto period,
        CancellationToken ct = default);
}
