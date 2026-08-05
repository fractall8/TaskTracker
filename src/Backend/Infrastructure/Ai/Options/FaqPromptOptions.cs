namespace Infrastructure.Ai.Options;

// Prompt text lives in configuration because wording is the main tuning lever for answer quality.
public class FaqPromptOptions
{
    public const string SectionName = "FaqPrompt";

    public string SystemPrompt { get; set; } = string.Empty;

    // Returned verbatim, so the model is never called just to produce an "I don't know".
    public string NoContextReply { get; set; } = string.Empty;

    // A correctness control, not tone: without it the model asserts a plan or role it cannot know.
    public string ConditionalAnswerInstruction { get; set; } = string.Empty;

    // Runs with no retrieved documentation, so it must forbid product claims outright.
    public string ConversationalPrompt { get; set; } = string.Empty;

    // Shown when Azure OpenAI blocks the request under its content policy — a refusal, not an outage.
    public string BlockedReply { get; set; } = string.Empty;

    // Appended when the workspace-data tools are available. Fences tool output separately from
    // documentation, and relaxes the conditional-answer rule only for facts a tool actually returned.
    public string DataToolInstruction { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(SystemPrompt)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(NoContextReply))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(NoContextReply)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(ConditionalAnswerInstruction))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ConditionalAnswerInstruction)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(ConversationalPrompt))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ConversationalPrompt)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(BlockedReply))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(BlockedReply)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(DataToolInstruction))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DataToolInstruction)} is not configured.");
        }
    }
}
