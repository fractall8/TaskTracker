namespace Domain.Entities;

public class Tag : BaseEntity<Guid>
{
    public required string Name { get; set; }

    public required string Color { get; set; }

    public required Guid WorkspaceId { get; init; }

    public Workspace? Workspace { get; set; }

    public ICollection<TaskTag> TaskTags { get; set; } = [];
}
