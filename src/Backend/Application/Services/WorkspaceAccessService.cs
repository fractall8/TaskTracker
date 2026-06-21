using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Enums;

namespace Application.Services;

public class WorkspaceAccessService(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository) : IWorkspaceAccessService
{
    public async Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new { Id = (Guid?)u.Id },
            ct);

        if (userInfo?.Id == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return userInfo.Id.Value;
    }

    public async Task<WorkspaceRole> EnsureIsMemberAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);

        var role = await workspaceRepository.GetUserRoleAsync(workspaceId, userId, ct);

        if (role == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace.");
        }

        return role.Value;
    }

    public async Task EnsureIsAdminAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var role = await EnsureIsMemberAsync(workspaceId, ct);

        if (role != WorkspaceRole.Admin)
        {
            throw new UnauthorizedAccessException("You must be a Workspace Admin to perform this action.");
        }
    }
}
