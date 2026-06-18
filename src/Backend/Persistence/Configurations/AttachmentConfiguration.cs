using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(AttachmentConstants.MaxFileNameLength);

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(AttachmentConstants.MaxFileUrlLength);
            
        builder.Property(a => a.ContentType)
            .HasMaxLength(AttachmentConstants.MaxContentTypeLength);
        
        builder.HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(a => a.TaskId);
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}