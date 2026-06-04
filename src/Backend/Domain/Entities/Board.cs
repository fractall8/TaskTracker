namespace Domain.Entities;

public class Board : BaseEntity<Guid>
{
    public string Name { get; set; }
    
    public string? Description { get; set; }
    
    public ICollection<Column> Columns { get; set; } = [];
}