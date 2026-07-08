using Contracts.Enums;

namespace Contracts.DTOs;

public record BoardMemberDto(
    Guid WorkspaceMemberId,
    Guid UserId,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    BoardRoleDto BoardRole,
    WorkspaceRoleDto WorkspaceRole,
    DateTimeOffset JoinedAt
);
