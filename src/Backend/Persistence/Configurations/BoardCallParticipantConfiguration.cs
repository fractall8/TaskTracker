using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class BoardCallParticipantConfiguration : IEntityTypeConfiguration<BoardCallParticipant>
{
    public void Configure(EntityTypeBuilder<BoardCallParticipant> builder)
    {
        builder.ToTable("BoardCallParticipants");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.BoardCallId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.JoinedAt)
            .IsRequired();

        builder.HasIndex(p => new { p.BoardCallId, p.UserId })
            .IsUnique()
            .HasFilter("\"LeftAt\" IS NULL");

        builder.HasIndex(p => p.BoardCallId);

        builder.HasOne(p => p.BoardCall)
            .WithMany(c => c.Participants)
            .HasForeignKey(p => p.BoardCallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.ToTable(t => t.HasCheckConstraint("CK_BoardCallParticipants_LeftAfterJoined", "\"LeftAt\" IS NULL OR \"LeftAt\" >= \"JoinedAt\""));
    }
}
