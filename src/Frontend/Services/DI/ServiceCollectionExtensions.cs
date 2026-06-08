using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Services.Abstractions.Auth;
using Services.Abstractions.Boards;
using Services.Api;
using Services.Auth;
using Services.Auth.Stores;
using Services.Boards;
using Services.Configuration;

namespace Services.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiClientOptions>(configuration.GetSection(ApiClientOptions.SectionName));
        var options = configuration.GetSection(ApiClientOptions.SectionName).Get<ApiClientOptions>()!;

        services.AddScoped<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
        
        services.AddRefitClient<IBoardApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddScoped<IAuthApiService, AuthApiService>();
        services.AddScoped<IProfileStore, ProfileStore>();
        
        services.AddScoped<IBoardApiService, BoardApiService>();

        return services;
    }
}