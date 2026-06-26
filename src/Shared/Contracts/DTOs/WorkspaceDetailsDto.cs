using Contracts.Enums;

namespace Contracts.DTOs;

public record WorkspaceDetailsDto(Guid Id, string Name, string? Description, WorkspaceRoleDto UserRole, List<WorkspaceMemberDto> Members);
