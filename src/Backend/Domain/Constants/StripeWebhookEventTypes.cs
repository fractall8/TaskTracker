namespace Domain.Constants;


public static class StripeWebhookEventTypes
{
    public const string CustomerSubscriptionCreated = "customer.subscription.created";
    public const string CustomerSubscriptionDeleted = "customer.subscription.deleted";
    public const string CustomerSubscriptionUpdated = "customer.subscription.updated";
}
