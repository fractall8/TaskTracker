using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> builder)
    {
        builder.ToTable("TaskTags");
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Task)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TaskId, e.TagId });
        builder.HasIndex(e => e.TagId);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
