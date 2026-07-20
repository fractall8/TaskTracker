using System.Text.Json.Serialization.Metadata;
using Infrastructure.Common.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI.Modules;

public static class SignalRModule
{
    public static IServiceCollection AddSignalRModule(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicJsonModifier.AddBoardActionPolymorphism }
                };
            });

        return services;
    }
}
