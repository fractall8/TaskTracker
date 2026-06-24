using Domain.Enums;

namespace Domain.Entities;

public class WorkspaceMember : BaseEntity<Guid>
{
    public required Guid WorkspaceId { get; init; }

    public required Guid UserId { get; init; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public WorkspaceRole Role { get; set; }

    public Workspace? Workspace { get; set; }

    public User? User { get; set; }
}
