namespace Application.Options;

public class FaqChatOptions
{
    public const string SectionName = "FaqChat";

    public int MaxQuestionLength { get; set; } = 1000;

    /// <summary>
    /// Counts individual messages, not exchanges: 12 is six question-and-answer pairs.
    /// </summary>
    public int MaxHistoryTurns { get; set; } = 12;

    public int RateLimitPermitsPerWindow { get; set; } = 10;

    public int RateLimitWindowSeconds { get; set; } = 60;
}
