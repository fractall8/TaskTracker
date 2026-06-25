using Contracts.Enums;

namespace Contracts.Requests.Workspaces;

public class ChangeMemberRoleRequest
{
    public WorkspaceRoleDto Role { get; set; }
}
