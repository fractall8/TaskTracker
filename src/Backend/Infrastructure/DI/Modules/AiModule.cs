using Application.Interfaces.Services;
using Application.Options;
using Azure;
using Azure.Search.Documents;
using Infrastructure.Ai;
using Infrastructure.Ai.Options;
using Infrastructure.Ai.Tools;
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

        services.AddOptions<AiToolOptions>()
            .BindConfiguration(AiToolOptions.SectionName)
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

        services.AddScoped<IFaqKnowledgeSearch, FaqKnowledgeSearch>();
        services.AddScoped<IFaqAssistantService, FaqAssistantService>();

        // Scoped so one request cannot spend another's tool budget.
        services.AddScoped<FaqToolPlugin>();
        services.AddScoped(sp => new AiToolBudget(
            sp.GetRequiredService<IOptions<AiToolOptions>>().Value.MaxToolCallsPerTurn));
        services.AddScoped<FaqToolInvocationFilter>();

        return services;
    }
}
