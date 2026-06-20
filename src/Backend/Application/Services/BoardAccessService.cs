using Application.Common.Models;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Authorization;
using Domain.Enums;

namespace Application.Services;

public class BoardAccessService(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository) : IBoardAccessService
{
    public async Task<(Guid UserId, string Email)> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new { Id = (Guid?)u.Id, u.Email },
            ct);

        if (userInfo?.Id == null || string.IsNullOrEmpty(userInfo.Email))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return (userInfo.Id.Value, userInfo.Email);
    }

    public Task<BoardAccessContext> EnsureCanEditBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanEditBoard, "You don't have permission to edit this board.", ct);

    public Task<BoardAccessContext> EnsureCanDeleteBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanDeleteBoard, "You don't have permission to delete this board.", ct);

    public Task<BoardAccessContext> EnsureCanManageColumnsAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageColumns, "You don't have permission to manage columns.", ct);

    public Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageTasks, "You don't have permission to manage tasks.", ct);

    public Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, _ => true, "You are not a member of this board.", ct);

    private async Task<BoardAccessContext> EnsureAccessAsync(Guid boardId, Func<BoardRole, bool> permissionCheck, string errorMessage, CancellationToken ct)
    {
        var (userId, _) = await GetCurrentUserAsync(ct);

        var userRole = await boardRepository.GetUserRoleAsync(boardId, userId, ct);

        if (userRole == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this board.");
        }

        if (!permissionCheck(userRole.Value))
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        return new BoardAccessContext(userId, userRole.Value);
    }
}
