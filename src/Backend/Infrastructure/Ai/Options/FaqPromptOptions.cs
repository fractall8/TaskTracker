namespace Infrastructure.Ai.Options;

/// <summary>
/// All assistant prompt text lives in configuration rather than as string literals: prompt wording is
/// the main tuning lever for answer quality, and needing a rebuild to adjust a sentence makes that
/// iteration painfully slow.
/// </summary>
public class FaqPromptOptions
{
    public const string SectionName = "FaqPrompt";

    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Returned verbatim when retrieval finds nothing above the relevance floor, so the model is never
    /// called just to produce an "I don't know".
    /// </summary>
    public string NoContextReply { get; set; } = string.Empty;

    /// <summary>
    /// A correctness control, not tone guidance. The assistant has no access to the requester's plan,
    /// workspace, or role, so without this an unconstrained model will confidently assert one anyway.
    /// </summary>
    public string ConditionalAnswerInstruction { get; set; } = string.Empty;

    /// <summary>
    /// Used only for messages answerable from the conversation itself. Runs with no retrieved documentation
    /// at all, so it must forbid product claims outright rather than relying on grounding to constrain them.
    /// </summary>
    public string ConversationalPrompt { get; set; } = string.Empty;

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
    }
}
