namespace Application.Interfaces.Services;

public interface IBoardExportSchedulerJob
{
    Task RunAsync(CancellationToken ct);
}
