namespace Infrastructure.Ai.Options;

public class AzureAiSearchOptions
{
    public const string SectionName = "AzureAiSearch";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string SemanticConfigurationName { get; set; } = string.Empty;

    // Portal-wizard-generated names: configuration, not constants — re-running it changes them.
    public string VectorFieldName { get; set; } = string.Empty;

    public string ContentFieldName { get; set; } = string.Empty;

    public string TitleFieldName { get; set; } = string.Empty;

    public int TopK { get; set; }

    // Reranker score (0–4), not cosine similarity — similarity is uncalibrated across corpora.
    // Calibrated live: in-corpus 2.40–3.21, off-corpus 0.43–1.02, an injection probe 1.57. Hence 2.0.
    public double MinRerankerScore { get; set; }

    // The vectorizer's embedding deployment 404s under load (seen as 502); nothing retries that by default.
    public int MaxRetryAttempts { get; set; }

    public int RetryDelayMilliseconds { get; set; }

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

        if (string.IsNullOrWhiteSpace(IndexName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(IndexName)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(SemanticConfigurationName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(SemanticConfigurationName)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(VectorFieldName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(VectorFieldName)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(ContentFieldName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ContentFieldName)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(TitleFieldName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(TitleFieldName)} is not configured.");
        }

        if (TopK <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(TopK)} must be greater than 0.");
        }

        if (MinRerankerScore < 0 || MinRerankerScore > 4)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(MinRerankerScore)} must be between 0 and 4 (semantic reranker scale).");
        }

        if (MaxRetryAttempts < 1)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(MaxRetryAttempts)} must be at least 1.");
        }

        if (RetryDelayMilliseconds < 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(RetryDelayMilliseconds)} must not be negative.");
        }
    }
}
