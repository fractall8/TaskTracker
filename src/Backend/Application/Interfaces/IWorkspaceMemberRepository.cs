using Application.Interfaces;
using Domain.Entities;

public interface IWorkspaceMemberRepository : IRepository<WorkspaceMember, Guid>
{
    Task<WorkspaceMember?> GetByWorkspaceAndUserIdAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);
}
