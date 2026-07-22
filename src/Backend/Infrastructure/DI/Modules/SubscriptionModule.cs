using Application.Interfaces.Services;
using Infrastructure.Services;
using Infrastructure.Subscriptions.Options;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

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

        services.AddSingleton<IStripeClient>(sp =>
        {
            var stripeOptions = sp.GetRequiredService<StripeOptions>();

            return new StripeClient(stripeOptions.SecretKey);
        });

        services.AddScoped<ISubscriptionService, StripeSubscriptionsService>();

        return services;
    }
}
