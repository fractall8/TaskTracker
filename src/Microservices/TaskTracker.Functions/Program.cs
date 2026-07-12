using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TaskTracker.Functions.ExternalProviders.Api;
using TaskTracker.Functions.ExternalProviders.Blob;
using TaskTracker.Functions.ExternalProviders.CosmosDB;
using TaskTracker.Functions.Archiving;
using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Processing;
using TaskTracker.Functions.Processing.Export;

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

    .AddHttpClient<IBoardExportStatusNotifyApiClient, BoardExportStatusNotifyApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<BoardExportApiClientOptions>>().Value;
        options.Validate();

        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        client.Timeout = TimeSpan.FromMinutes(options.RequestTimeoutMinutes);
    })
    .Services

    .Configure<BlobStorageOptions>(
        builder.Configuration.GetSection(BlobStorageOptions.SectionName))
    .AddSingleton(sp =>
    {
        var connectionString = builder.Configuration.GetConnectionString("BlobStorage")
            ?? throw new InvalidOperationException("ConnectionStrings:BlobStorage is not configured.");

        return new BlobServiceClient(connectionString);
    })
    .AddSingleton<IBoardExportBlobService, BoardExportBlobService>()

    .Configure<CosmosDbOptions>(
        builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .AddSingleton(sp =>
    {
        var connectionString = builder.Configuration.GetConnectionString("CosmosDB")
            ?? throw new InvalidOperationException("ConnectionStrings:CosmosDB is not configured.");

        return new CosmosClient(connectionString);
    })
    .AddSingleton(sp =>
    {
        var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
        options.Validate();

        return sp.GetRequiredService<CosmosClient>()
            .GetContainer(options.DatabaseName, options.Containers.BoardExport);
    })
    .AddSingleton<IBoardExportDocumentClient, CosmosBoardExportDocumentClient>()


    .AddSingleton<IBoardExportSummaryWriter, JsonBoardExportSummaryWriter>()
    .AddSingleton<BoardExportSummaryWriterRegistry>()
    .AddSingleton<IBoardArchiveBuilder, BoardArchiveBuilder>()

    .AddSingleton<ExportContextResolver>()
    .AddSingleton<IBoardExportCompletionHandler, InitialExportCompletionHandler>()
    .AddSingleton<IBoardExportCompletionHandler, ReExportCompletionHandler>()
    .AddSingleton<BoardExportCompletionHandlerRegistry>()
    .AddSingleton<IBoardExportProcessor, BoardExportProcessor>();

var host = builder.Build();
await host.RunAsync();
