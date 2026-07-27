using Application.Interfaces.Services;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Domain.Constants;
using Infrastructure.Boards.Calls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI.Modules;

internal static class AcsModule
{
    public static IServiceCollection AddAcsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var acsConnectionString = configuration.GetConnectionString(ConnectionStrings.AzureCommunicationServices);

        if (string.IsNullOrWhiteSpace(acsConnectionString))
        {
            throw new InvalidOperationException("Azure Communication Services connection string not found");
        }

        services.AddSingleton(_ => new CommunicationIdentityClient(acsConnectionString));
        services.AddSingleton(_ => new RoomsClient(acsConnectionString));

        services.AddScoped<IAcsCallService, AcsCallService>();

        return services;
    }
}
