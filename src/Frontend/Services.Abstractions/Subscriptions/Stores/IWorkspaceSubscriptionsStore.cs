using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.Subscriptions.Stores;

public interface IWorkspaceSubscriptionsStore
{
    IReadOnlyList<PlanCardDto> Plans { get; }

    SubscriptionDetailsDto? Subscription { get; }

    bool IsLoading { get; }

    string? ErrorMessage { get; }

    PaymentConfirmationStatusDto PaymentStatus { get; }

    event Action? StateChanged;

    Task LoadBillingDataAsync(Guid workspaceId, CancellationToken ct = default);

    Task ConfirmPurchasedPlanAsync(Guid workspaceId, string expectedPlanId, CancellationToken ct = default);

    void Reset();
}
