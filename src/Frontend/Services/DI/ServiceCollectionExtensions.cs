using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Services.Abstractions.Auth;
using Services.Abstractions.Boards;
using Services.Abstractions.Columns;
using Services.Abstractions.Tasks;
using Services.Abstractions.Workspaces;
using Services.Api;
using Services.Auth;
using Services.Auth.Stores;
using Services.Boards;
using Services.Boards.Stores;
using Services.Columns;
using Services.Configuration;
using Services.Tasks;
using Services.Workspaces;
using Services.Workspaces.Stores;

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

        services.AddRefitClient<IColumnsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<ITasksApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IWorkspaceApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
        };

        services.AddRefitClient<IWorkspaceMembersApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IWorkspaceInvitesApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddScoped<IAuthApiService, AuthApiService>();
        services.AddScoped<IProfileStore, ProfileStore>();

        services.AddScoped<IBoardApiService, BoardApiService>();
        services.AddScoped<IBoardStore, BoardStore>();

        services.AddScoped<IColumnApiService, ColumnApiService>();
        services.AddScoped<ITaskApiService, TaskApiService>();
        services.AddScoped<IBoardDetailsStore, BoardDetailsStore>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IWorkspaceStore, WorkspaceStore>();
        services.AddScoped<IWorkspaceApiService, WorkspaceApiService>();

        return services;
    }
}
