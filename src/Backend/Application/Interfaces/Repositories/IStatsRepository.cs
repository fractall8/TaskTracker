using Application.Common.Models;

namespace Application.Interfaces.Repositories;

public interface IStatsRepository
{
    Task<StatsCounts> GetCountsAsync(
        Guid workspaceId,
        StatsWindow window,
        DateTimeOffset asOf,
        CancellationToken ct = default);

    Task<List<TaskCompletionSample>> GetCompletionSamplesAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default);

    Task<List<DateTimeOffset>> GetCreationTimestampsAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
