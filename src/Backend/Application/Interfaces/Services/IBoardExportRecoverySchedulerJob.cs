namespace Application.Interfaces.Services;

public interface IBoardExportRecoverySchedulerJob
{
    Task RunAsync(CancellationToken ct);
}
