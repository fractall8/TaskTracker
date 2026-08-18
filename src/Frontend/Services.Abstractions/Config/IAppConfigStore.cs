namespace Services.Abstractions.Config;

public interface IAppConfigStore
{
    // Minutes east of UTC for the deployment's business zone. Zero until loaded.
    int UtcOffsetMinutes { get; }

    Task EnsureLoadedAsync(CancellationToken ct = default);
}
