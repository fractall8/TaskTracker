namespace Domain.Entities;

public class TaskItem : BaseEntity
{
    public Guid ColumnId { get; set; }
    
    public string Title { get; set; }
    
    public string? Description { get; set; }
    
    public int Postion { get; set; }
    
    public Column Column { get; set; }
}