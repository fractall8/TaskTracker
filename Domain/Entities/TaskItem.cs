namespace Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; }
    
    public string? Description { get; set; }
    
    public int Position { get; set; }
 
    public Guid ColumnId { get; set; }
    
    public Column Column { get; set; }
}