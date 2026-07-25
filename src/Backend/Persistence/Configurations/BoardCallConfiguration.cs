using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class BoardCallConfiguration : IEntityTypeConfiguration<BoardCall>
{
    public void Configure(EntityTypeBuilder<BoardCall> builder)
    {
        builder.ToTable("BoardCalls");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.BoardId)
            .IsRequired();

        builder.Property(c => c.StartedByUserId)
            .IsRequired();

        builder.Property(c => c.AcsRoomId)
            .IsRequired()
            .HasMaxLength(BoardCallConstants.MaxAcsRoomIdLength);

        builder.Property(c => c.StartedAt)
            .IsRequired();

        builder.HasIndex(c => c.BoardId)
            .IsUnique()
            .HasFilter("\"EndedAt\" IS NULL");

        builder.HasIndex(c => c.AcsRoomId)
            .IsUnique();

        builder.HasOne(c => c.Board)
            .WithMany()
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.BoardCall)
            .HasForeignKey(p => p.BoardCallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.ToTable(t => t.HasCheckConstraint("CK_BoardCalls_EndedAfterStarted", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\""));
        builder.ToTable(t => t.HasCheckConstraint("CK_BoardCalls_AcsRoomId_NotEmpty", "btrim(\"AcsRoomId\") <> ''"));
    }
}
