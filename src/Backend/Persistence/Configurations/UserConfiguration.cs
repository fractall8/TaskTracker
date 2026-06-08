using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.AzureAdObjectId)
            .IsUnique();

        builder.Property(e => e.AzureAdObjectId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}