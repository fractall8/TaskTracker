namespace Contracts.DTOs;

public record BoardMemberDto(
    Guid WorkspaceMemberId,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    Contracts.Enums.BoardRoleDto Role,
    DateTimeOffset JoinedAt
);
