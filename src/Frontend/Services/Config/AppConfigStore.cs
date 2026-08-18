using Services.Abstractions.Config;
using Services.Api;
using Services.Extensions;

namespace Services.Config;

internal sealed class AppConfigStore(IAppConfigApi configApi) : IAppConfigStore
{
    private bool _loaded;

    public int UtcOffsetMinutes { get; private set; }

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded)
        {
            return;
        }

        try
        {
            var response = await configApi.GetAsync(ct);
            var config = await response.HandleResponseAsync();

            UtcOffsetMinutes = config.CurrentUtcOffsetMinutes;
            _loaded = true;
        }
        catch
        {
            // A failed config fetch must not break a task page: the badge falls back to UTC, which is the
            // same guess the assistant used to make.
        }
    }
}
