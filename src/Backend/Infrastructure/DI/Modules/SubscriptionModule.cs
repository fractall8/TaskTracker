using Application.Interfaces.Services;
using Infrastructure.Subscriptions.Options;
using Infrastructure.Subscriptions.Services;
using Infrastructure.Subscriptions.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            var stripeOptions = sp.GetRequiredService<IOptions<StripeOptions>>().Value;

            return new StripeClient(stripeOptions.SecretKey);
        });

        services.AddScoped<ISubscriptionService, StripeSubscriptionsService>();
        services.AddSingleton<IPlanCatalog, PlanCatalog>();

        services.AddScoped<ISubscriptionWebhookEventHandler, CustomerSubscriptionCreatedWebhookHandler>();
        services.AddScoped<ISubscriptionWebhookEventHandler, CustomerSubscriptionUpdatedWebhookHandler>();
        services.AddScoped<ISubscriptionWebhookEventHandler, CustomerSubscriptionDeletedWebhookHandler>();

        services.AddScoped<IWorkspaceEntitlementService, WorkspaceEntitlementService>();
        services.AddScoped<IWorkspaceLimitService, WorkspaceLimitService>();

        return services;
    }
}
