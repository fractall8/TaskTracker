namespace Application.Interfaces.Services;

public interface IBoardCallLifecycleService
{
    Task EndCallAsync(Guid boardCallId, Guid? endedByUserId = null, CancellationToken ct = default);

    Task EndCallIfEmptyAsync(Guid boardCallId, CancellationToken ct = default);
}
