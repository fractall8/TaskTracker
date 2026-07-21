namespace Domain.Entities;

public class Subscription : BaseEntity<Guid>
{
    public required Guid UserId { get; set; }

    public required string PlanId { get; set; }

    public required string StripeCustomerId { get; set; }

    public required string StripeSubscriptionId { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset? CurrentPeriodStartAt { get; set; }

    public DateTimeOffset? CurrentPeriodEndAt { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public User? User { get; set; }
}
