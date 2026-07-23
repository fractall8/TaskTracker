using Contracts.DTOs;
using Contracts.Enums;
using Services.Abstractions.Subscriptions;
using Services.Abstractions.Subscriptions.Stores;

namespace Services.Subscriptions.Stores;

public class WorkspaceSubscriptionsStore(ISubscriptionApiService subscriptionApiService) : IWorkspaceSubscriptionsStore
{
    public IReadOnlyList<PlanCardDto> Plans { get; private set; } = [];
    public SubscriptionDetailsDto? Subscription { get; private set; }

    public EntitlementDto? Entitlements { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }
    public PaymentConfirmationStatusDto PaymentStatus { get; private set; } = PaymentConfirmationStatusDto.Idle;

    public event Action? StateChanged;
    private void NotifyStateChanged() => StateChanged?.Invoke();

    public bool HasFeature(string featureName)
    {
        return Entitlements?.Features.Contains(featureName, StringComparer.OrdinalIgnoreCase) == true;
    }

    public async Task LoadBillingDataAsync(Guid workspaceId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var subTask = subscriptionApiService.GetSubscriptionAsync(workspaceId, ct);
            var plansTask = subscriptionApiService.GetPlansAsync(workspaceId, ct);
            var entitlementsTask = subscriptionApiService.GetEntitlementsAsync(workspaceId, ct);

            await Task.WhenAll(subTask, plansTask);

            Subscription = subTask.Result;
            Plans = plansTask.Result;
            Entitlements = entitlementsTask.Result;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task ConfirmPurchasedPlanAsync(Guid workspaceId, string expectedPlanId, CancellationToken ct = default)
    {
        PaymentStatus = PaymentConfirmationStatusDto.Confirming;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            for (int i = 0; i < 5; i++)
            {
                var entitlements = await subscriptionApiService.GetEntitlementsAsync(workspaceId, ct);

                if (entitlements.PlanId.Equals(expectedPlanId, StringComparison.OrdinalIgnoreCase))
                {
                    PaymentStatus = PaymentConfirmationStatusDto.Confirmed;
                    NotifyStateChanged();
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }

            PaymentStatus = PaymentConfirmationStatusDto.AwaitingActivation;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            PaymentStatus = PaymentConfirmationStatusDto.AwaitingActivation;
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void Reset()
    {
        Plans = [];
        Subscription = null;
        Entitlements = null;
        IsLoading = false;
        ErrorMessage = null;
        PaymentStatus = PaymentConfirmationStatusDto.Idle;
        NotifyStateChanged();
    }
}
