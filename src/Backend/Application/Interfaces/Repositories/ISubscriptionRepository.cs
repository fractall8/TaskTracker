using Contracts.DTOs;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ISubscriptionRepository : IRepository<Subscription, Guid>
{
    Task<string?> GetUserPlanIdAsync(Guid userId, CancellationToken ct = default);

    Task<SubscriptionDto?> GetSubscriptionByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);

    Task<bool> ExistsByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default);

    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default);

    Task<string?> GetBillableStripeCustomerIdAsync(Guid userId, CancellationToken ct = default);
}
