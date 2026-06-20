namespace Domain.Entities;

public class Attachment : BaseEntity<Guid>
{
    public required string FileName { get; set; }

    public required string FileUrl { get; set; }

    public long SizeInBytes { get; set; }

    public required string ContentType { get; set; }

    public required Guid TaskId { get; set; }

    public TaskItem? Task { get; set; }
}
