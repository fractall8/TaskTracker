namespace Contracts.DTOs;

public record BoardMemberDto(
    Guid WorkspaceMemberId,
    Guid UserId,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    Contracts.Enums.BoardRoleDto Role,
    DateTimeOffset JoinedAt
);
