using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TaskTracker.Functions.ExternalProviders.Api;
using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Processing;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .Configure<BoardExportApiClientOptions>(
        builder.Configuration.GetSection(BoardExportApiClientOptions.SectionName))

    .AddHttpClient<IBoardExportDataApiClient, BoardExportDataApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<BoardExportApiClientOptions>>().Value;
        options.Validate();

        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        client.Timeout = TimeSpan.FromMinutes(options.RequestTimeoutMinutes);
    })

    .Services
    .AddScoped<IBoardExportProcessor, BoardExportProcessor>();

var host = builder.Build();
await host.RunAsync();
