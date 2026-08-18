using Application.Common.Models;
using Contracts.DTOs;

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

    Task<List<StatsBoardDto>> GetBoardBreakdownAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default);

    Task<List<StatsTagDto>> GetTagBreakdownAsync(Guid workspaceId, CancellationToken ct = default);

    Task<int> CountUntaggedOpenTasksAsync(Guid workspaceId, CancellationToken ct = default);

    Task<List<StatsWorkloadDto>> GetWorkloadAsync(
        Guid workspaceId,
        DateTimeOffset asOf,
        CancellationToken ct = default);

    Task<List<StatsUserCount>> GetReportedCountsAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default);

    Task<List<StatsUserCount>> GetCompletedCountsAsync(
        Guid workspaceId,
        DateTimeOffset? from,
        DateTimeOffset to,
        CancellationToken ct = default);

    Task<List<OverdueTaskRow>> GetOverdueTasksAsync(
        Guid workspaceId,
        DateTimeOffset overdueBefore,
        int take,
        CancellationToken ct = default);
}
