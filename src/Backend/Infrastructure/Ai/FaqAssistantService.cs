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
        try
        {
            return await AnswerAsync(question, history, ct);
        }
        catch (ContentFilteredException)
        {
            return new FaqAnswerDto(_prompt.BlockedReply, FaqAnswerKindDto.Unsupported, Citations: []);
        }
    }

    private async Task<FaqAnswerDto> AnswerAsync(
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
        CancellationToken ct)
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

        // A tool-answered turn carries no citations. Retrieval runs on every question and usually returns
        // something above the relevance floor, but those chunks did not produce the answer — attaching them
        // credited "Feature: Share Your Screen in a Call" for a list of tasks.
        if (toolBudget.Used > 0)
        {
            return new FaqAnswerDto(answer, FaqAnswerKindDto.DataBacked, Citations: []);
        }

        if (chunks.Count > 0)
        {
            return new FaqAnswerDto(answer, FaqAnswerKindDto.Grounded, BuildCitations(chunks));
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

        LogTurnCost(FaqTurnUsage.Measure(chatHistory, completion));

        if (string.IsNullOrWhiteSpace(completion?.Content))
        {
            throw new ExternalServiceUnavailableException(
                "The assistant could not generate a response. Please try again in a moment.");
        }

        return completion.Content.Trim();
    }

    // Counts only — the message contents themselves are never logged.
    private void LogTurnCost(FaqTurnUsage usage) =>
        logger.LogInformation(
            "FAQ turn cost: {ModelCalls} model call(s), {ToolCalls}/{ToolBudget} tool call(s), "
            + "{InputTokens} input and {OutputTokens} output token(s), of which {ReasoningTokens} reasoning.",
            usage.ModelCalls,
            toolBudget.Used,
            toolBudget.MaxCalls,
            usage.InputTokens,
            usage.OutputTokens,
            usage.ReasoningTokens);

    private async Task<ChatMessageContent?> CompleteAsync(
        ChatHistory chatHistory,
        Kernel activeKernel,
        CancellationToken ct)
    {
        var chat = activeKernel.GetRequiredService<IChatCompletionService>();

        try
        {
            return await chat.GetChatMessageContentAsync(
                chatHistory,
                BuildExecutionSettings(activeKernel),
                activeKernel,
                ct);
        }
        catch (Exception ex) when (IsContentFiltered(ex))
        {
            logger.LogInformation("FAQ request blocked by the Azure OpenAI content filter.");

            throw new ContentFilteredException(ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FAQ chat completion failed.");

            throw new ExternalServiceUnavailableException(
                "The assistant is temporarily unavailable. Please try again in a moment.", ex);
        }
    }

    // Azure returns 400 content_filter for blocked prompts, including its jailbreak shield. That is a
    // refusal, so it must not be reported as an outage.
    private static bool IsContentFiltered(Exception exception) =>
        exception is HttpOperationException { StatusCode: System.Net.HttpStatusCode.BadRequest }
        && exception.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase);

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
