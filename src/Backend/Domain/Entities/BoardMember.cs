using Domain.Enums;

namespace Domain.Entities;

public class BoardMember : BaseEntity<Guid>
{
    public required Guid BoardId { get; init; }

    public required Guid UserId { get; init; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.Now;

    public BoardRole Role { get; set; }

    public Board? Board { get; set; }

    public User? User { get; set; }
}
