namespace Domain.Entities;

public class BoardCall : BaseEntity<Guid>
{
    public required Guid BoardId { get; set; }

    public required Guid StartedByUserId { get; set; }

    public required string AcsRoomId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public Board? Board { get; set; }

    public ICollection<BoardCallParticipant> Participants { get; set; } = [];
}
