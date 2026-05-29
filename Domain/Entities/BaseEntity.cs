namespace Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public Guid? CreatedById { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
   
    public Guid? UpdatedById { get; set; }
    
    public DateTimeOffset? DeletedAt { get; set; }
    
    public Guid? DeletedById { get; set; }
    
    public bool IsDeleted { get; set; }
}