using Domain.Enums;

namespace Application.Interfaces.Services;

public interface IWorkspaceAccessService
{
    Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default);

    Task<WorkspaceRole> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default);

    Task EnsureIsAdminAsync(Guid workspaceId, CancellationToken ct = default);
}
