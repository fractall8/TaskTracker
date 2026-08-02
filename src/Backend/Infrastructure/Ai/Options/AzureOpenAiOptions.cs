namespace Infrastructure.Ai.Options;

public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAi";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChatDeploymentName { get; set; } = string.Empty;

    public int MaxOutputTokens { get; set; }

    /// <summary>
    /// Leave null for reasoning models (gpt-5*, o*), which reject any non-default temperature —
    /// null means the parameter is omitted from the request entirely rather than sent as a default.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Reasoning models only: minimal, low, medium, or high. Null omits the parameter.
    /// </summary>
    public string? ReasoningEffort { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Endpoint)} is not configured.");
        }

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Endpoint)} must be a valid absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ApiKey)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(ChatDeploymentName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ChatDeploymentName)} is not configured.");
        }

        if (MaxOutputTokens <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(MaxOutputTokens)} must be greater than 0.");
        }

        // Range-checked only when set — an unset value is a valid, meaningful configuration.
        if (Temperature is < 0f or > 2f)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Temperature)} must be between 0 and 2.");
        }

        if (ReasoningEffort is { Length: > 0 } effort
            && effort is not ("minimal" or "low" or "medium" or "high"))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(ReasoningEffort)} must be one of: minimal, low, medium, high.");
        }
    }
}
