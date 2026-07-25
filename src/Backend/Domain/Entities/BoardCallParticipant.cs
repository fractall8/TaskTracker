namespace Domain.Entities;

public class BoardCallParticipant : BaseEntity<Guid>
{
    public required Guid BoardCallId { get; set; }

    public required Guid UserId { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset? LeftAt { get; set; }

    public BoardCall? BoardCall { get; set; }

    public User? User { get; set; }
}
