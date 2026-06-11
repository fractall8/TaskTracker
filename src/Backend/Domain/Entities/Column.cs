namespace Domain.Entities;

public class Column : BaseEntity<Guid>
{
    public required string Name { get; set; }
    
    public int Position { get; set; }
    
    public required Guid BoardId { get; init; }
    
    public Board? Board { get; set; }
    
    public ICollection<TaskItem> Tasks { get; set; } = [];
}