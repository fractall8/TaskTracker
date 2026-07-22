using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class StripeWebhookEventRepository(TaskTrackerDbContext dbContext)
    : Repository<StripeWebhookEvent, Guid>(dbContext), IStripeWebhookEventRepository
{
    public async Task<StripeWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken ct = default)
    {
        return await DbContext.StripeWebhookEvents.FirstOrDefaultAsync(x => x.EventId == eventId, ct);
    }
}
