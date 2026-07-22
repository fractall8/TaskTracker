using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> builder)
    {
        builder.Property(e => e.EventId)
            .HasMaxLength(StripeWebhookConstants.MaxEventIdLength)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(StripeWebhookConstants.MaxEventTypeLength)
            .IsRequired();

        builder.HasIndex(e => e.EventId)
            .IsUnique();

        builder.HasIndex(e => e.ReceivedAt)
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
