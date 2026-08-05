namespace Application.Options;

public class FaqChatOptions
{
    public const string SectionName = "FaqChat";

    public int MaxQuestionLength { get; set; } = 1000;

    // Applies to replayed turns, which include the assistant's own answers — those are bounded by
    // AzureOpenAi:MaxOutputTokens, not by what a user can type. Reusing MaxQuestionLength here made any
    // answer over 1000 characters poison the rest of the conversation. Raise this if MaxOutputTokens rises.
    public int MaxHistoryTurnLength { get; set; } = 20000;

    /// <summary>
    /// Counts individual messages, not exchanges: 12 is six question-and-answer pairs.
    /// </summary>
    public int MaxHistoryTurns { get; set; } = 12;

    public int RateLimitPermitsPerWindow { get; set; } = 10;

    public int RateLimitWindowSeconds { get; set; } = 60;
}
