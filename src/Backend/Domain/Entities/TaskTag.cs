namespace Domain.Entities;

public class TaskTag : BaseEntity<Guid>
{
    public required Guid TaskId { get; init; }

    public TaskItem? Task { get; set; }

    public required Guid TagId { get; init; }

    public Tag? Tag { get; set; }
}
