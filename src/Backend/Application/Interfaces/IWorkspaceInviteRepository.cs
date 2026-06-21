using Domain.Entities;

namespace Application.Interfaces;

public interface IWorkspaceInviteRepository : IRepository<WorkspaceInvite, Guid>
{
    Task<WorkspaceInvite?> GetByTokenAsync(string token, CancellationToken ct = default);
}
