using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.Property(s => s.PlanId).HasMaxLength(SubscriptionConstants.MaxPlanIdLength);
        builder.Property(s => s.StripeCustomerId).HasMaxLength(SubscriptionConstants.MaxStripeCustomerIdLength);
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(SubscriptionConstants.MaxStripeSubscriptionIdLength);
        builder.Property(s => s.Status).HasMaxLength(SubscriptionConstants.MaxStatusLength);
        builder.Property(s => s.CancelAtPeriodEnd).HasDefaultValue(false);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.StripeSubscriptionId)
            .IsUnique();

        builder.HasOne(s => s.Workspace)
            .WithMany()
            .HasForeignKey(s => s.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.WorkspaceId)
            .IsUnique()
            .HasFilter(BuildSubscriptionStatusFilter());

        builder.HasIndex(s => s.UserId);

        builder.HasIndex(s => s.StripeCustomerId);
    }

    private static string BuildSubscriptionStatusFilter()
    {
        var statuses = string.Join(", ", SubscriptionStatus.AllBillable.Select(s => $"'{s}'"));
        return $"\"Status\" IN ({statuses})";
    }
}
