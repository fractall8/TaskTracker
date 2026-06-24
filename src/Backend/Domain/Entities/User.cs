namespace Domain.Entities;

public class User : BaseEntity<Guid>
{
    public required Guid AzureAdObjectId { get; init; }

    public required string Email { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }
}
