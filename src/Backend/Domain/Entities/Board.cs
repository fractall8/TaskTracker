namespace Domain.Entities;

public class Board : BaseEntity<Guid>
{
    public required Guid WorkspaceId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public Workspace? Workspace { get; set; }

    public ICollection<BoardMember> Members { get; set; } = [];

    public ICollection<Column> Columns { get; set; } = [];
}
