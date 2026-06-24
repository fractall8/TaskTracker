namespace Domain.Entities;

public class WorkspaceInvite : BaseEntity<Guid>
{
    public required Guid WorkspaceId { get; init; }

    public required string Token { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }

    public Workspace? Workspace { get; set; }
}
