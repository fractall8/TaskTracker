using Domain.Constants;
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

        builder.Property(e => e.AcsCommunicationUserId)
            .HasMaxLength(BoardCallConstants.MaxAcsCommunicationUserIdLength);

        builder.HasIndex(e => e.AcsCommunicationUserId)
            .IsUnique()
            .HasFilter("\"AcsCommunicationUserId\" IS NOT NULL");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.ToTable(t => t.HasCheckConstraint("CK_Users_AcsCommunicationUserId_NotEmpty", "\"AcsCommunicationUserId\" IS NULL OR btrim(\"AcsCommunicationUserId\") <> ''"));
    }
}
