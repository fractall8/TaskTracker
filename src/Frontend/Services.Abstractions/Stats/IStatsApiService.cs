using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.Stats;

public interface IStatsApiService
{
    Task<WorkspaceStatsDto> GetStatsAsync(Guid workspaceId, StatsPeriodDto period, CancellationToken ct = default);
}
