using Infrastructure.Boards.Export;
using Microsoft.Extensions.Options;

namespace Presentation.Middlewares;

public class InternalApiKeyMiddleware(RequestDelegate next, IOptions<InternalApiOptions> settings)
{
    private static readonly PathString _internalPathPrefix = new("/api/internal");

    public async Task InvokeAsync(HttpContext context)
    {
        var options = settings.Value;

        if (!context.Request.Path.StartsWithSegments(_internalPathPrefix))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(options.ApiKeyHeaderName, out var providedKey)
            || !string.Equals(providedKey, options.ApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
