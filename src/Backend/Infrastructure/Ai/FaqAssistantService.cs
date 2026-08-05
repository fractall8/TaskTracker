using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Contracts.Enums;
using Domain.Exceptions;
using Infrastructure.Ai.Options;
using Infrastructure.Ai.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Infrastructure.Ai;

internal class FaqAssistantService(
    Kernel kernel,
    IFaqKnowledgeSearch knowledgeSearch,
    FaqToolPlugin toolPlugin,
    AiToolBudget toolBudget,
    FaqToolInvocationFilter toolFilter,
    IOptions<AzureOpenAiOptions> openAiOptions,
    IOptions<FaqPromptOptions> promptOptions,
    IOptions<AiToolOptions> toolOptions,
    ILogger<FaqAssistantService> logger) : IFaqAssistantService
{
    private const int _maxCitationExcerptLength = 220;
    private const string _untitledSourceFallback = "TaskTracker documentation";

    private readonly AzureOpenAiOptions _openAi = openAiOptions.Value;
    private readonly FaqPromptOptions _prompt = promptOptions.Value;
    private readonly AiToolOptions _tools = toolOptions.Value;

    public async Task<FaqAnswerDto> AskAsync(
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
        CancellationToken ct = default)
    {
        // Checked first only because it is cheap; the patterns are whole-message anchored, so a question
        // with a greeting attached still reaches retrieval.
        if (FaqConversationIntent.IsConversational(question))
        {
            logger.LogDebug("FAQ message answerable from the conversation; skipping retrieval.");

            var reply = await GenerateAsync(_prompt.ConversationalPrompt, question, history, kernel, ct);

            return new FaqAnswerDto(reply, FaqAnswerKindDto.Conversational, Citations: []);
        }

        var chunks = await knowledgeSearch.SearchAsync(question, ct);

        // Zero chunks no longer means refusal: a question about the caller's own data legitimately matches
        // no documentation but is answerable by a tool (EPIC 3 Decision 9).
        var toolKernel = _tools.Enabled ? BuildToolKernel() : kernel;

        var answer = await GenerateAsync(
            FaqPromptBuilder.BuildSystemPrompt(_prompt, chunks, _tools.Enabled),
            question,
            history,
            toolKernel,
            ct);

        var citations = chunks.Count > 0 ? BuildCitations(chunks) : [];

        // Tools take precedence over documentation: when both contributed, the definitive statements came
        // from the caller's own data, and labelling that Grounded would attribute them to a doc citation.
        // Citations are still returned when documentation also contributed.
        if (toolBudget.Used > 0)
        {
            return new FaqAnswerDto(answer, FaqAnswerKindDto.DataBacked, citations);
        }

        if (chunks.Count > 0)
        {
            return new FaqAnswerDto(answer, FaqAnswerKindDto.Grounded, citations);
        }

        // Neither documentation nor a tool supported an answer, so whatever the model produced is
        // ungrounded — discard it rather than relay an assertion nothing backs.
        logger.LogInformation("FAQ question matched no documentation and no tool; returning the no-context reply.");

        return new FaqAnswerDto(_prompt.NoContextReply, FaqAnswerKindDto.Unsupported, Citations: []);
    }

    // The registered Kernel is a singleton, so filters and plugins must go on a per-request clone —
    // otherwise one caller's tool budget would be shared with every other caller.
    private Kernel BuildToolKernel()
    {
        var scoped = kernel.Clone();

        scoped.FunctionInvocationFilters.Add(toolFilter);
        scoped.Plugins.AddFromObject(toolPlugin, FaqToolPlugin.PluginName);

        return scoped;
    }

    private async Task<string> GenerateAsync(
        string systemPrompt,
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
        Kernel activeKernel,
        CancellationToken ct)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        foreach (var turn in history)
        {
            if (string.IsNullOrWhiteSpace(turn.Content))
            {
                continue;
            }

            if (turn.Role == FaqChatRoleDto.Assistant)
            {
                chatHistory.AddAssistantMessage(turn.Content);
            }
            else
            {
                chatHistory.AddUserMessage(turn.Content);
            }
        }

        chatHistory.AddUserMessage(question.Trim());

        var completion = await CompleteAsync(chatHistory, activeKernel, ct);

        if (string.IsNullOrWhiteSpace(completion))
        {
            throw new ExternalServiceUnavailableException(
                "The assistant could not generate a response. Please try again in a moment.");
        }

        return completion.Trim();
    }

    private async Task<string?> CompleteAsync(ChatHistory chatHistory, Kernel activeKernel, CancellationToken ct)
    {
        var chat = activeKernel.GetRequiredService<IChatCompletionService>();

        try
        {
            var result = await chat.GetChatMessageContentAsync(
                chatHistory,
                BuildExecutionSettings(activeKernel),
                activeKernel,
                ct);
            return result.Content;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FAQ chat completion failed.");

            throw new ExternalServiceUnavailableException(
                "The assistant is temporarily unavailable. Please try again in a moment.", ex);
        }
    }

    private AzureOpenAIPromptExecutionSettings BuildExecutionSettings(Kernel activeKernel)
    {
#pragma warning disable SKEXP0010 // SetNewMaxCompletionTokensEnabled: sends max_completion_tokens instead
        // of max_tokens, which reasoning models (gpt-5*, o*) require. Verified against gpt-5-mini.
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            SetNewMaxCompletionTokensEnabled = true,
            MaxTokens = _openAi.MaxOutputTokens
        };
#pragma warning restore SKEXP0010

        // Both are sent only when configured: reasoning models reject any explicit temperature.
        if (_openAi.Temperature is { } temperature)
        {
            settings.Temperature = temperature;
        }

        if (!string.IsNullOrWhiteSpace(_openAi.ReasoningEffort))
        {
            settings.ReasoningEffort = _openAi.ReasoningEffort;
        }

        // Offered only when the kernel actually carries the plugin, so the conversational path never sees
        // tools. Sequential invocation is intentional: AiToolBudget is not thread-safe.
        if (activeKernel.Plugins.Count > 0)
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                options: new FunctionChoiceBehaviorOptions { AllowConcurrentInvocation = false });
        }

        return settings;
    }

    private static List<FaqCitationDto> BuildCitations(IReadOnlyList<RetrievedChunk> chunks) =>
        [.. chunks.Select(chunk => new FaqCitationDto(
            string.IsNullOrWhiteSpace(chunk.Title) ? _untitledSourceFallback : chunk.Title,
            Excerpt(chunk.Content)))];

    private static string Excerpt(string content)
    {
        var collapsed = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length <= _maxCitationExcerptLength
            ? collapsed
            : string.Concat(collapsed.AsSpan(0, _maxCitationExcerptLength).TrimEnd(), "…");
    }
}
