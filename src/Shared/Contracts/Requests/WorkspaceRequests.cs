using Contracts.Enums;

namespace Contracts.Requests;

public record InviteUserRequest(string? Email);

public record AcceptInviteRequest(string Token);

public record CreateWorkspaceRequest(string Name, string? Description);

public record UpdateWorkspaceRequest(string Name, string? Description);

public record ChangeMemberRoleRequest(WorkspaceRoleDto Role);
