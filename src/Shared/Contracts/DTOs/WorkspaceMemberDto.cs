using Contracts.Enums;

namespace Contracts.DTOs;

public record WorkspaceMemberDto(
    Guid Id,
    Guid UserId,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    WorkspaceRoleDto Role,
    DateTimeOffset JoinedAt
);
