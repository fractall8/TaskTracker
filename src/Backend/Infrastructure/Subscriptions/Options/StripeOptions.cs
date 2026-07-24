namespace Infrastructure.Subscriptions.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException($"{SectionName}:SecretKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(WebhookSecret))
        {
            throw new InvalidOperationException($"{SectionName}:WebhookSecret is not configured.");
        }

        if (string.IsNullOrWhiteSpace(SuccessUrl))
        {
            throw new InvalidOperationException($"{SectionName}:SuccessUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(CancelUrl))
        {
            throw new InvalidOperationException($"{SectionName}:CancelUrl is not configured.");
        }
    }
}
