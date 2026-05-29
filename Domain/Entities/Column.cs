namespace Domain.Entities;

public class Column : BaseEntity
{
    public string Name { get; set; }
    
    public int Position { get; set; }
    
    public Guid BoardId { get; set; }
    
    public Board Board { get; set; }
    
    public ICollection<TaskItem> Tasks { get; set; } = [];
}