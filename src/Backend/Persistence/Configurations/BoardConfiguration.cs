using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("Boards");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.WorkspaceId)
            .IsRequired();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(BoardConstants.MaxNameLength);

        builder.HasQueryFilter(e => !e.IsDeleted && !e.IsArchived);
    }
}
