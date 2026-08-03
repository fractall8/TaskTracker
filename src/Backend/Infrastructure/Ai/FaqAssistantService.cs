using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using Domain.Exceptions;
using Infrastructure.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Infrastructure.Ai;

internal class FaqAssistantService(
    Kernel kernel,
    IFaqKnowledgeSearch knowledgeSearch,
    IOptions<AzureOpenAiOptions> openAiOptions,
    IOptions<FaqPromptOptions> promptOptions,
    ILogger<FaqAssistantService> logger) : IFaqAssistantService
{
    private const int _maxCitationExcerptLength = 220;
    private const string _untitledSourceFallback = "TaskTracker documentation";

    private readonly AzureOpenAiOptions _openAi = openAiOptions.Value;
    private readonly FaqPromptOptions _prompt = promptOptions.Value;

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

            var reply = await GenerateAsync(_prompt.ConversationalPrompt, question, history, ct);

            return new FaqAnswerDto(reply, FaqAnswerKindDto.Conversational, Citations: []);
        }

        var chunks = await knowledgeSearch.SearchAsync(question, ct);

        if (chunks.Count == 0)
        {
            logger.LogInformation("FAQ question had no grounding context; returning the no-context reply.");
            return new FaqAnswerDto(_prompt.NoContextReply, FaqAnswerKindDto.Unsupported, Citations: []);
        }

        var answer = await GenerateAsync(
            FaqPromptBuilder.BuildSystemPrompt(_prompt, chunks),
            question,
            history,
            ct);

        return new FaqAnswerDto(answer, FaqAnswerKindDto.Grounded, BuildCitations(chunks));
    }

    private async Task<string> GenerateAsync(
        string systemPrompt,
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
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

        var completion = await CompleteAsync(chatHistory, ct);

        if (string.IsNullOrWhiteSpace(completion))
        {
            throw new ExternalServiceUnavailableException(
                "The assistant could not generate a response. Please try again in a moment.");
        }

        return completion.Trim();
    }

    private async Task<string?> CompleteAsync(ChatHistory chatHistory, CancellationToken ct)
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        try
        {
            var result = await chat.GetChatMessageContentAsync(chatHistory, BuildExecutionSettings(), kernel, ct);
            return result.Content;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FAQ chat completion failed.");

            throw new ExternalServiceUnavailableException(
                "The assistant is temporarily unavailable. Please try again in a moment.", ex);
        }
    }

    private AzureOpenAIPromptExecutionSettings BuildExecutionSettings()
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
