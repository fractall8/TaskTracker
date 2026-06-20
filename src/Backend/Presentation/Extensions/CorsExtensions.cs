using Domain.Constants;
using Presentation.Options;

namespace Presentation.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddPresentationCors(this IServiceCollection services, IConfiguration configuration)
    {
        var frontendOptions = configuration.GetSection(FrontendOptions.SectionName).Get<FrontendOptions>();
        var origins = frontendOptions?.AllowedOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.DefaultCorsPolicy, policyBuilder =>
            {
                policyBuilder
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                if (origins is not null && origins.Length > 0)
                {
                    policyBuilder.WithOrigins(origins);
                }
                else
                {
                    policyBuilder.AllowAnyOrigin();
                }
            });
        });

        return services;
    }
}
