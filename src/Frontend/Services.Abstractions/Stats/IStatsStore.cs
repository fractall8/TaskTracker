using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.Stats;

public interface IStatsStore
{
    Guid? WorkspaceId { get; }
    WorkspaceStatsDto? Stats { get; }
    StatsPeriodDto Period { get; }
    bool IsLoading { get; }
    string? ErrorMessage { get; }

    event Action? StateChanged;

    Task LoadAsync(Guid workspaceId, CancellationToken ct = default);

    Task ChangePeriodAsync(StatsPeriodDto period, CancellationToken ct = default);

    Task RefreshAsync(CancellationToken ct = default);

    void Reset();
}
