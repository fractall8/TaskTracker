using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Domain.Constants;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI.Modules;

internal static class BlobModule
{
    public static IServiceCollection AddBlobModule(this IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetConnectionString(ConnectionStrings.AzureBlobStorageConnection);
        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));

        services.AddScoped<IFileService, BlobStorageService>();

        return services;
    }
}
