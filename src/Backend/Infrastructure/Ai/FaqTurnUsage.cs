using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatTokenUsage = OpenAI.Chat.ChatTokenUsage;

namespace Infrastructure.Ai;

// Cost of one question. Auto-invocation makes several model round trips and returns only the last message,
// so usage is summed from the chat history Semantic Kernel appends to as it goes.
internal readonly record struct FaqTurnUsage(int ModelCalls, int InputTokens, int OutputTokens, int ReasoningTokens)
{
    public static FaqTurnUsage Measure(ChatHistory history, ChatMessageContent? final)
    {
        // ChatMessageContent uses reference equality, and the final message is usually already in the
        // history — deduplicating avoids counting it twice.
        var seen = new HashSet<ChatMessageContent>();
        var usage = default(FaqTurnUsage);

        foreach (var message in history)
        {
            if (seen.Add(message))
            {
                usage += Read(message);
            }
        }

        if (final is not null && seen.Add(final))
        {
            usage += Read(final);
        }

        return usage;
    }

    public static FaqTurnUsage operator +(FaqTurnUsage left, FaqTurnUsage right)
    {
        return new FaqTurnUsage(
            left.ModelCalls + right.ModelCalls,
            left.InputTokens + right.InputTokens,
            left.OutputTokens + right.OutputTokens,
            left.ReasoningTokens + right.ReasoningTokens);
    }

    private static FaqTurnUsage Read(ChatMessageContent message) =>
        message.Metadata?.TryGetValue("Usage", out var raw) == true && raw is ChatTokenUsage tokens
            ? new FaqTurnUsage(
                1,
                tokens.InputTokenCount,
                tokens.OutputTokenCount,
                tokens.OutputTokenDetails?.ReasoningTokenCount ?? 0)
            : default;
}
