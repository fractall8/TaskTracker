using Contracts.Constants;

namespace Infrastructure.Subscriptions.Options;

public class PlanOptions
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public string? PriceId { get; set; }
    public string[] Features { get; set; } = [];

    internal void Validate(string sectionPath)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException($"{sectionPath}:{nameof(Id)} is not configured.");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidOperationException($"{sectionPath}:{nameof(DisplayName)} is not configured.");
        }

        if (Features is null)
        {
            throw new InvalidOperationException($"{sectionPath}:{nameof(Features)} is not configured.");
        }

        foreach (var feature in Features)
        {
            if (!FeatureConstants.IsValid(feature))
            {
                throw new InvalidOperationException(
                    $"{sectionPath}:{nameof(Features)} contains unknown feature '{feature}'. " +
                    $"Allowed: {string.Join(", ", FeatureConstants.GetAll())}");
            }
        }
    }
}
