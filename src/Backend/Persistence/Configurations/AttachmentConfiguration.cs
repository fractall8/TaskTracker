using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(AttachmentConstants.MaxFileNameLength);

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(AttachmentConstants.MaxFileUrlLength);
            
        builder.Property(a => a.ContentType)
            .HasMaxLength(AttachmentConstants.MaxContentTypeLength);
    }
}