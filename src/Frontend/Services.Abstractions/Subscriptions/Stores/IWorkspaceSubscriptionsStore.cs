using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.Subscriptions.Stores;

public interface IWorkspaceSubscriptionsStore
{
    IReadOnlyList<PlanCardDto> Plans { get; }

    SubscriptionDetailsDto? Subscription { get; }

    EntitlementDto? Entitlements { get; }

    SubscriptionLimitsDto? Limits { get; }

    bool IsLoading { get; }

    string? ErrorMessage { get; }

    PaymentConfirmationStatusDto PaymentStatus { get; }

    event Action? StateChanged;

    bool HasFeature(string featureName);

    Task LoadBillingDataAsync(Guid workspaceId, bool forceReload = false, CancellationToken ct = default);

    Task ConfirmPurchasedPlanAsync(Guid workspaceId, string expectedPlanId, CancellationToken ct = default);

    void Reset();
}
