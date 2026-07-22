using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IStripeWebhookEventRepository : IRepository<StripeWebhookEvent, Guid>
{
    Task<StripeWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken ct = default);
}
