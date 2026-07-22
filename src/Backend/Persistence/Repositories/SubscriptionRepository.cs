using Application.Interfaces.Repositories;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class SubscriptionRepository(TaskTrackerDbContext dbContext)
    : Repository<Subscription, Guid>(dbContext), ISubscriptionRepository
{
    public Task<string?> GetUserPlanIdAsync(Guid userId, CancellationToken ct = default) =>
        DbContext.Subscriptions
            .Where(s => s.UserId == userId && SubscriptionStatus.AllBillable.Contains(s.Status))
            .Select(s => s.PlanId)
            .FirstOrDefaultAsync(ct);

    public Task<SubscriptionDto?> GetSubscriptionByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        DbContext.Subscriptions
            .Where(s => s.WorkspaceId == workspaceId && SubscriptionStatus.AllBillable.Contains(s.Status))
            .Select(s => new SubscriptionDto(
                s.PlanId,
                s.Status,
                s.CancelAtPeriodEnd,
                s.CurrentPeriodStartAt,
                s.CurrentPeriodEndAt))
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsByStripeSubscriptionIdAsync(
        string stripeSubscriptionId,
        CancellationToken ct = default) =>
        DbContext.Subscriptions.AnyAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);

    public Task<Subscription?> GetByStripeSubscriptionIdAsync(
        string stripeSubscriptionId,
        CancellationToken ct = default) =>
        DbContext.Subscriptions.FirstOrDefaultAsync(
            s => s.StripeSubscriptionId == stripeSubscriptionId,
            ct);

    public Task<string?> GetBillableStripeCustomerIdAsync(Guid userId, CancellationToken ct = default) =>
        DbContext.Subscriptions
            .Where(s => s.UserId == userId && SubscriptionStatus.AllBillable.Contains(s.Status))
            .Select(s => s.StripeCustomerId)
            .FirstOrDefaultAsync(ct);
}
