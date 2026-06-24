namespace Domain.Entities;

public class Workspace : BaseEntity<Guid>
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<Board> Boards { get; set; } = [];

    public ICollection<WorkspaceMember> Members { get; set; } = [];
}
