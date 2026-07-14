namespace Infrastructure.Boards.Export;

public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public required string ApiKeyHeaderName { get; init; }
    public required string ApiKey { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyHeaderName))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ApiKeyHeaderName)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ApiKey)} is not configured.");
        }
    }
}
