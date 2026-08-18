using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Services.Abstractions.Auth;
using Services.Abstractions.BoardCalls;
using Services.Abstractions.FaqChat;
using Services.Abstractions.Boards;
using Services.Abstractions.Columns;
using Services.Abstractions.Hubs;
using Services.Abstractions.Profile;
using Services.Abstractions.Subscriptions;
using Services.Abstractions.Subscriptions.Stores;
using Services.Abstractions.Stats;
using Services.Abstractions.Tags;
using Services.Abstractions.Tasks;
using Services.Abstractions.Workspaces;
using Services.Api;
using Services.Auth;
using Services.Auth.Stores;
using Services.BoardCalls;
using Services.BoardCalls.Stores;
using Services.FaqChat;
using Services.FaqChat.Stores;
using Services.Boards;
using Services.Boards.Stores;
using Services.Columns;
using Services.Configuration;
using Services.Hubs;
using Services.Profile;
using Services.Subscriptions;
using Services.Subscriptions.Stores;
using Services.Stats;
using Services.Stats.Stores;
using Services.Tags;
using Services.Tags.Stores;
using Services.Tasks;
using Services.Tasks.Stores;
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

        services.AddRefitClient<IBoardMembersApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IColumnsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<ITasksApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IStatsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<ITagsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IWorkspaceApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IProfileApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IBoardCallsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddRefitClient<IFaqChatApi>()
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

        services.AddRefitClient<ISubscriptionsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.BaseUrl))
            .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

        services.AddScoped<ISubscriptionApiService, SubscriptionApiService>();
        services.AddScoped<IWorkspaceSubscriptionsStore, WorkspaceSubscriptionsStore>();

        services.AddScoped<IAuthApiService, AuthApiService>();
        services.AddScoped<IProfileStore, ProfileStore>();
        services.AddScoped<IProfileApiService, ProfileApiService>();

        services.AddScoped<IBoardApiService, BoardApiService>();
        services.AddScoped<IBoardStore, BoardStore>();

        services.AddScoped<IBoardMembersStore, BoardMembersStore>();
        services.AddScoped<IBoardMembersApiService, BoardMembersApiService>();

        services.AddScoped<IColumnApiService, ColumnApiService>();
        services.AddScoped<ITaskApiService, TaskApiService>();
        services.AddScoped<IBoardDetailsStore, BoardDetailsStore>();

        services.AddScoped<ITaskDetailsStore, TaskDetailsStore>();

        services.AddScoped<IStatsApiService, StatsApiService>();
        services.AddScoped<IStatsStore, StatsStore>();

        services.AddScoped<ITagApiService, TagApiService>();
        services.AddScoped<ITagStore, TagStore>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IWorkspaceStore, WorkspaceStore>();
        services.AddScoped<IWorkspaceApiService, WorkspaceApiService>();

        services.AddScoped<IFaqChatApiService, FaqChatApiService>();
        services.AddScoped<IFaqChatStore, FaqChatStore>();

        services.AddScoped<IBoardExportStatusHubService, BoardExportStatusHubService>();

        services.AddScoped<IBoardActionSyncGuard, BoardActionSyncGuard>();
        services.AddScoped<IBoardActionsHubService, BoardActionsHubService>();

        services.AddScoped<IBoardCallApiService, BoardCallApiService>();
        services.AddScoped<IBoardCallStore, BoardCallStore>();
        services.AddScoped<IAcsCallInteropService, AcsCallInteropService>();

        return services;
    }
}
