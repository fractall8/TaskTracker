using Contracts.Enums;

namespace Contracts.DTOs;

// The resolved window is echoed back so the client can label the chart with the range the server actually
// used, rather than re-deriving it and risking a mismatch. Both bounds carry the caller's UTC offset.
public record WorkspaceStatsDto(
    StatsPeriodDto Period,
    DateTimeOffset? PeriodStart,
    DateTimeOffset PeriodEnd,
    StatsSummaryDto Summary,
    StatsTrendDto Trend,
    List<StatsBoardDto> Boards,
    List<StatsTagDto> Tags,
    List<StatsWorkloadDto> Workload,
    List<StatsContributorDto> Contributors,
    StatsOverdueDto Overdue);
