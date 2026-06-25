using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.ToTable("BoardMembers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.JoinedAt)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(m => new { m.BoardId, m.WorkspaceMemberId })
            .IsUnique();

        builder.HasOne(m => m.WorkspaceMember)
            .WithMany()
            .HasForeignKey(m => m.WorkspaceMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Board)
            .WithMany(b => b.Members)
            .HasForeignKey(m => m.BoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
