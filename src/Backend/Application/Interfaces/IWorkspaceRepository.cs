using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IWorkspaceRepository : IRepository<Workspace, Guid>
{
    Task<bool> ExistsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    Task<List<Workspace>> GetUserWorkspacesAsync(Guid userId, CancellationToken ct = default);
}
