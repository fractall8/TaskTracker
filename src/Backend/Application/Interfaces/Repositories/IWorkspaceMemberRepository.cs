using Contracts.DTOs;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IWorkspaceMemberRepository : IRepository<WorkspaceMember, Guid>
{
    Task<WorkspaceMember?> GetByWorkspaceAndUserIdAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    Task<List<WorkspaceMemberDto>> SearchWorkspaceUsersAsync(Guid workspaceId, string? searchTerm, CancellationToken ct = default);
}