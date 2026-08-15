using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(TagConstants.MaxNameLength);
        builder.Property(e => e.Color).IsRequired().HasMaxLength(TagConstants.ColorLength);

        builder.HasOne(e => e.Workspace)
            .WithMany(w => w.Tags)
            .HasForeignKey(e => e.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Uniqueness is case-insensitive and lives in 0023_AddTags.sql, which EF cannot express.
        builder.HasIndex(e => new { e.WorkspaceId, e.Name });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
