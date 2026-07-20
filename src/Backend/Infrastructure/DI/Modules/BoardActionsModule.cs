using System.Text.Json.Serialization.Metadata;
using Application.Interfaces.Notifiers;
using Contracts.Notifications.BoardActions;
using Contracts.Serialization;
using Infrastructure.Boards.Notifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI.Modules;

public static class BoardActionsModule
{
    public static IServiceCollection AddBoardActionsModule(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicJsonModifier.AddBoardActionPolymorphism }
                };
            });

        services.AddScoped<IBoardActionNotifier, BoardActionNotifier>();

        return services;
    }
}
