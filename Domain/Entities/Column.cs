namespace Domain.Entities;

public class Column : BaseEntity
{
    public Guid BoardId { get; set; }
    
    public string Name { get; set; }
    
    public int Postion { get; set; }
    
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    
    public Board Board { get; set; }
}