using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IWorkspaceInviteRepository : IRepository<WorkspaceInvite, Guid>
{
    Task<WorkspaceInvite?> GetByTokenAsync(string token, CancellationToken ct = default);

    Task<List<WorkspaceInvite>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
}
