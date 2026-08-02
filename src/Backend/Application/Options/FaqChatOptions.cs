namespace Application.Options;

public class FaqChatOptions
{
    public const string SectionName = "FaqChat";

    public int MaxQuestionLength { get; set; } = 1000;

    public int MaxHistoryTurns { get; set; } = 6;
}
