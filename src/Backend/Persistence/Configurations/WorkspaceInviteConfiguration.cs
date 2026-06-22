using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class WorkspaceInviteConfiguration : IEntityTypeConfiguration<WorkspaceInvite>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvite> builder)
    {
        builder.ToTable("WorkspaceInvites");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .HasMaxLength(WorkspaceConstants.MaxEmailLength);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(WorkspaceConstants.InviteTokenLength);

        builder.HasIndex(e => e.Token)
            .IsUnique();

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.HasOne(e => e.Workspace)
            .WithMany()
            .HasForeignKey(e => e.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(x => new { x.WorkspaceId, x.Email })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"Email\" IS NOT NULL");
    }
}
