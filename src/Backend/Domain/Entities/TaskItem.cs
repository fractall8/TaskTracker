namespace Domain.Entities;

public class TaskItem : BaseEntity<Guid>
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public int Position { get; set; } = 0;

    public DateTimeOffset? DueDate { get; set; }

    // Independent of the column; the three fields move together (CK_Tasks_Completion_Consistent).
    public bool IsCompleted { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Guid? CompletedById { get; set; }

    public required Guid ColumnId { get; set; }

    public Guid? AssigneeId { get; set; }

    public required Guid ReporterId { get; init; }

    public Column? Column { get; set; }

    public User? Assignee { get; set; }

    public User? CompletedBy { get; set; }

    public User? Reporter { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
