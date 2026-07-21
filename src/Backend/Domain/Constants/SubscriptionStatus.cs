namespace Domain.Constants;


public static class SubscriptionStatus
{
    public const string Incomplete = "incomplete";
    public const string IncompleteExpired = "incomplete_expired";
    public const string Trialing = "trialing";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Canceled = "canceled";
    public const string Unpaid = "unpaid";
    public const string Paused = "paused";

    private static readonly HashSet<string> _billableSet =
    [
        Active,
        Trialing,
        PastDue,
    ];

    private static readonly HashSet<string> _documentedStatusesSet =
    [
        Incomplete,
        IncompleteExpired,
        Trialing,
        Active,
        PastDue,
        Canceled,
        Unpaid,
        Paused,
    ];

    public static bool IsBillable(string status) =>
        !string.IsNullOrEmpty(status) && _billableSet.Contains(status);

    public static bool IsDocumentedStatus(string status) =>
        !string.IsNullOrEmpty(status) && _documentedStatusesSet.Contains(status);

    public static IReadOnlyCollection<string> AllBillable => _billableSet;
}
