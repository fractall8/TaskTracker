using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Domain.Constants;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetConnectionString(ConnectionStrings.AzureBlobStorageConnection);
        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
        
        services.AddScoped<IFileService, BlobStorageService>();
        
        return services;
    }
}