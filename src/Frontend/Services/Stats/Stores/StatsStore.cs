using Contracts.DTOs;
using Contracts.Enums;
using Services.Abstractions.Stats;

namespace Services.Stats.Stores;

internal sealed class StatsStore(IStatsApiService apiService) : IStatsStore
{
    public Guid? WorkspaceId { get; private set; }
    public WorkspaceStatsDto? Stats { get; private set; }

    // Thirty days, so a quiet workspace still looks alive on first open (EPIC 5 Decision 4a).
    public StatsPeriodDto Period { get; private set; } = StatsPeriodDto.Last30Days;

    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public async Task LoadAsync(Guid workspaceId, CancellationToken ct = default)
    {
        WorkspaceId = workspaceId;
        await FetchAsync(ct);
    }

    public async Task ChangePeriodAsync(StatsPeriodDto period, CancellationToken ct = default)
    {
        if (period == Period)
        {
            return;
        }

        Period = period;
        await FetchAsync(ct);
    }

    // Stats are not realtime: figures that move while they are being read are worse than figures with a
    // known age (EPIC 5 Decision 11).
    public Task RefreshAsync(CancellationToken ct = default) => FetchAsync(ct);

    public void Reset()
    {
        WorkspaceId = null;
        Stats = null;
        Period = StatsPeriodDto.Last30Days;
        IsLoading = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private async Task FetchAsync(CancellationToken ct)
    {
        if (WorkspaceId is not { } workspaceId)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            // The browser's offset, so day buckets land on the reader's own calendar.
            var offsetMinutes = (int)DateTimeOffset.Now.Offset.TotalMinutes;

            Stats = await apiService.GetStatsAsync(workspaceId, Period, offsetMinutes, ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
