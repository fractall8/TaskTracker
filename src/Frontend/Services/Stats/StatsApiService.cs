using Contracts.DTOs;
using Contracts.Enums;
using Services.Abstractions.Stats;
using Services.Api;
using Services.Extensions;

namespace Services.Stats;

public class StatsApiService(IStatsApi statsApi) : IStatsApiService
{
    public async Task<WorkspaceStatsDto> GetStatsAsync(
        Guid workspaceId,
        StatsPeriodDto period,
        CancellationToken ct = default)
    {
        var response = await statsApi.GetAsync(workspaceId, period, ct);
        return await response.HandleResponseAsync();
    }
}
