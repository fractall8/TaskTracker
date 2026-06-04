using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Domain.Constants;
using Infrastructure.Auth;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DI;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetConnectionString(ConnectionStrings.AzureBlobStorageConnection);
        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
        
        services.AddScoped<IFileService, BlobStorageService>();
        
        services.AddEntraIdAuthentication(configuration);
        
        return services;
    }
}