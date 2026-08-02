using Azure;
using Azure.Search.Documents;
using Infrastructure.Ai.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.DI.Modules;

internal static class AiModule
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddOptions<AzureOpenAiOptions>()
            .BindConfiguration(AzureOpenAiOptions.SectionName)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddOptions<AzureAiSearchOptions>()
            .BindConfiguration(AzureAiSearchOptions.SectionName)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddOptions<FaqPromptOptions>()
            .BindConfiguration(FaqPromptOptions.SectionName)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureAiSearchOptions>>().Value;

            return new SearchClient(
                new Uri(options.Endpoint),
                options.IndexName,
                new AzureKeyCredential(options.ApiKey));
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAiOptions>>().Value;

            return Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: options.ChatDeploymentName,
                    endpoint: options.Endpoint,
                    apiKey: options.ApiKey)
                .Build();
        });

        return services;
    }
}
