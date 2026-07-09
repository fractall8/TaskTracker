using Application.Common.Models;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.Enums;
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

    public Task<BoardAccessContext> EnsureCanManageCommentsAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageComments, "You don't have permission to create comments.", ct);

    public Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, _ => true, "You don't have access to this board.", ct);

    public Task<BoardAccessContext> EnsureCanExportBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, _ => true, "You don't have permission to export this board.", ct);

    public async Task<BoardRoleDto?> GetEffectiveBoardRoleAsync(Guid boardId, CancellationToken ct = default)
    {
        var (userId, _) = await GetCurrentUserAsync(ct);

        var explicitBoardRole = await boardRepository.GetUserRoleAsync(boardId, userId, ct);

        if (explicitBoardRole.HasValue)
        {
            return (BoardRoleDto)explicitBoardRole.Value;
        }

        return null;
    }

    private async Task<BoardAccessContext> EnsureAccessAsync(Guid boardId, Func<BoardRole, bool> permissionCheck, string errorMessage, CancellationToken ct)
    {
        var effectiveRoleDto = await GetEffectiveBoardRoleAsync(boardId, ct);

        if (effectiveRoleDto == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this board or it does not exist.");
        }

        var effectiveBoardRole = (BoardRole)effectiveRoleDto;

        if (!permissionCheck(effectiveBoardRole))
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        var (userId, _) = await GetCurrentUserAsync(ct);
        return new BoardAccessContext(userId, effectiveBoardRole);
    }
}
