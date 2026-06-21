using Contracts.Enums;

namespace Contracts.DTOs;

public record WorkspaceDto(Guid Id, string Name, string? Description, WorkspaceRoleDto Role);
