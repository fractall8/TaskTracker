using Contracts.Enums;

namespace Contracts.DTOs;

public record WorkspaceMemberDto(Guid UserId, string Email, WorkspaceRoleDto Role, DateTimeOffset JoinedAt);
