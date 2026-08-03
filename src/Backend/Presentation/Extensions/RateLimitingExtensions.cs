using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Application.Options;
using Infrastructure.Auth.Constants;

namespace Presentation.Extensions;

public static class RateLimitingExtensions
{
    public const string FaqChatPolicy = "faq-chat";

    public static IServiceCollection AddPresentationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var faqChat = configuration.GetSection(FaqChatOptions.SectionName).Get<FaqChatOptions>() ?? new FaqChatOptions();
        var window = TimeSpan.FromSeconds(faqChat.RateLimitWindowSeconds);

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(FaqChatPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                ResolvePartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = faqChat.RateLimitPermitsPerWindow,
                    Window = window,
                    QueueLimit = 0
                }));

            limiter.OnRejected = async (context, ct) =>
            {
                var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                    : (int)Math.Ceiling(window.TotalSeconds);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";
                context.HttpContext.Response.Headers.RetryAfter =
                    retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                // Shaped as ProblemDetails so the frontend surfaces Detail like any other API failure (AD-6).
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        type = "https://tools.ietf.org/html/rfc9110#section-15.5.29",
                        title = "Too many requests",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = $"Too many questions in a short time. Please wait {retryAfterSeconds} seconds and try again."
                    }),
                    ct);
            };
        });

        return services;
    }

    // Partitioning by Entra object id keeps one user from spending another's quota behind a shared IP.
    private static string ResolvePartitionKey(HttpContext context) =>
        context.User.FindFirst(EntraClaimTypes.ObjectId)?.Value
        ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
