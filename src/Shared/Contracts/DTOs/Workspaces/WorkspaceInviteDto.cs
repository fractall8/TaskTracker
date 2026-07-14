namespace Contracts.DTOs;

public record WorkspaceInviteDto(
    Guid Id,
    string Token,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
