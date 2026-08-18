using Application.Common.Interfaces;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Services;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Contracts.Constants;
using Domain.Constants;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Boards.Export;
using Infrastructure.Boards.Jobs;
using Infrastructure.Boards.Notifiers;
using Infrastructure.Common.Untils;
using Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.DI.Modules;

internal static class BoardExportJobModule
{
    public static IServiceCollection AddBoardExportJobModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BoardExportSchedulerOptions>()
            .BindConfiguration(BoardExportStrings.BoardExportScheduler)
            .Validate(opts => { opts.Validate(); return true; })
            .ValidateOnStart();

        services.AddOptions<BoardExportRecoverySchedulerOptions>()
            .BindConfiguration(BoardExportStrings.BoardExportRecoveryScheduler)
            .Validate(opts => { opts.Validate(); return true; })
            .ValidateOnStart();

        services.AddOptions<CosmosDbOptions>()
            .BindConfiguration(CosmosDbOptions.SectionName)
            .Validate(opts => { opts.Validate(); return true; })
            .ValidateOnStart();

        services.AddOptions<InternalApiOptions>()
            .BindConfiguration(InternalApiOptions.SectionName)
            .Validate(opts => { opts.Validate(); return true; })
            .ValidateOnStart();

        var serviceBusConnectionString = configuration.GetConnectionString(ConnectionStrings.ServiceBus);

        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddServiceBusClient(serviceBusConnectionString);

            clientBuilder.AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
            {
                var client = provider.GetRequiredService<ServiceBusClient>();
                return client.CreateSender(ServiceBusQueueNames.BoardArchivingQueue);
            });
        });

        var cosmosConnectionString = configuration.GetConnectionString(ConnectionStrings.CosmosDb);

        services.AddSingleton(sp => new CosmosClient(cosmosConnectionString));

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var cosmosOptions = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

            return client.GetContainer(cosmosOptions.DatabaseName, cosmosOptions.Containers.BoardExport);
        });

        services.AddScoped<IBoardExportQueueSender, AzureBoardExportQueueSender>();
        services.AddScoped<IBoardExportService, CosmosBoardExportService>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddOptions<BusinessCalendarOptions>()
            .BindConfiguration(BusinessCalendarOptions.SectionName)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddScoped<IBusinessCalendar, BusinessCalendar>();
        services.AddScoped<IBoardExportStatusNotifier, BoardExportStatusNotifier>();
        services.AddScoped<IBoardExportSchedulerJob, BoardExportSchedulerJob>();
        services.AddScoped<IBoardExportRecoverySchedulerJob, BoardExportRecoverySchedulerJob>();

        services.AddSignalR();

        services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(configuration.GetConnectionString(ConnectionStrings.PostgresConnection))));

        services.AddHangfireServer();

        return services;
    }
}
