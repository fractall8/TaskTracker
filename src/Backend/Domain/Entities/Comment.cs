namespace Domain.Entities;

public class Comment : BaseEntity<Guid>
{
    public required string Text { get; set; }

    public required Guid TaskId { get; set; }

    public required Guid AuthorId { get; set; }

    public User? Author { get; set; }

    public TaskItem? Task { get; set; }
}
