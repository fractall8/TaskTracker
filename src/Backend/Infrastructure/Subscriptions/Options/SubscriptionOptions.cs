namespace Infrastructure.Subscriptions.Options;

public class SubscriptionOptions
{
    public const string SectionName = "Subscription";
    public Dictionary<string, PlanOptions>? Plans { get; set; }
    public required string DefaultPlanId { get; set; }

    public void Validate()
    {
        if (Plans is null || Plans.Count == 0)
        {
            throw new InvalidOperationException($"{SectionName}:Plans is not configured.");
        }

        if (string.IsNullOrWhiteSpace(DefaultPlanId))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(DefaultPlanId)} is not configured.");
        }

        if (!Plans.ContainsKey(DefaultPlanId))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(DefaultPlanId)} '{DefaultPlanId}' was not found in Plans.");
        }

        foreach (var (planKey, plan) in Plans)
        {
            plan.Validate($"{SectionName}:Plans:{planKey}");

            if (planKey != plan.Id)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:Plans:{planKey} key mismatch. The inner Id property must be '{planKey}', but found '{plan.Id}'.");
            }
        }
    }
}


