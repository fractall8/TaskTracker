using Infrastructure.Subscriptions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI.Modules;

internal static class SubscriptionModule
{
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services)
    {
        services.AddOptions<SubscriptionOptions>()
            .BindConfiguration(SubscriptionOptions.SectionName)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddOptions<StripeOptions>()
            .BindConfiguration(StripeOptions.SectionName)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        return services;
    }
}
