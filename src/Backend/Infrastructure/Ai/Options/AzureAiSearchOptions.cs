namespace Infrastructure.Ai.Options;

public class AzureAiSearchOptions
{
    public const string SectionName = "AzureAiSearch";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string SemanticConfigurationName { get; set; } = string.Empty;

    /// <summary>
    /// Field and configuration names are whatever the portal's import wizard generated — they are
    /// configuration, not constants, because re-running the wizard can produce different names.
    /// </summary>
    public string VectorFieldName { get; set; } = string.Empty;

    public string ContentFieldName { get; set; } = string.Empty;

    public string TitleFieldName { get; set; } = string.Empty;

    public int TopK { get; set; }

    /// <summary>
    /// Minimum semantic reranker score (0–4 scale) for a chunk to be treated as relevant. This is
    /// deliberately not a cosine-similarity threshold: similarity scores are uncalibrated and shift
    /// with corpus and embedding model, while the reranker score is comparable across queries.
    /// Calibrated against the live index: in-corpus questions score 2.40–3.21, off-corpus 0.43–1.02,
    /// and a prompt-injection probe scored 1.57 — hence a floor of 2.0 rather than the original 1.5.
    /// </summary>
    public double MinRerankerScore { get; set; }

    /// <summary>
    /// Retry attempts for a search call. The embedding deployment behind the index's vectorizer
    /// returns 404 under concurrent load (surfacing as 502 from Search), and neither the Search SDK
    /// nor typical resilience defaults retry those — a 404 is normally a permanent error.
    /// </summary>
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
