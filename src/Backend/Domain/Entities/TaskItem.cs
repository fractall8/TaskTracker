namespace Domain.Entities;

public class TaskItem : BaseEntity<Guid>
{
    public required string Title { get; set; }
    
    public string? Description { get; set; }

    public int Position { get; set; } = 0;
    
    public DateTimeOffset? DueDate { get; set; }
 
    public required Guid ColumnId { get; set; }
    
    public Guid? AssigneeId { get; set; }
    
    public required Guid ReporterId { get; init; }
    
    public Column? Column { get; set; }
    
    public User? Assignee { get; set; }
    
    public User? Reporter { get; set; }
    
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}