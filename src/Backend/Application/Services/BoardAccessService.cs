using Application.Common.Models;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.Enums;
using Domain.Authorization;
using Domain.Enums;
using Domain.Exceptions;

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
            throw new ForbiddenException("User is not authenticated.");
        }

        return (userInfo.Id.Value, userInfo.Email);
    }

    public Task<BoardAccessContext> EnsureCanEditBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanEditBoard, "You don't have permission to edit this board.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanDeleteBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanDeleteBoard, "You don't have permission to delete this board.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanManageColumnsAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageColumns, "You don't have permission to manage columns.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageTasks, "You don't have permission to manage tasks.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanCompleteTasksAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanCompleteTasks, "You don't have permission to complete tasks.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanTagTasksAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanTagTasks, "You don't have permission to tag tasks.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanManageCommentsAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageComments, "You don't have permission to create comments.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanManageAttachmentsAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageAttachments, "You don't have permission to manage attachments.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanViewBoard, "You don't have access to this board.", requiresActiveBoard: false, ct);

    public Task<BoardAccessContext> EnsureCanExportBoardAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, _ => true, "You don't have permission to export this board.", requiresActiveBoard: false, ct);

    public Task<BoardAccessContext> EnsureCanStartCallAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageCall, "You don't have permission to start a call on this board.", requiresActiveBoard: true, ct);

    public Task<BoardAccessContext> EnsureCanEndCallAsync(Guid boardId, CancellationToken ct = default) =>
        EnsureAccessAsync(boardId, BoardRolePermissions.CanManageCall, "You don't have permission to end this call.", requiresActiveBoard: true, ct);

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

    private async Task<BoardAccessContext> EnsureAccessAsync(
        Guid boardId,
        Func<BoardRole, bool> permissionCheck,
        string errorMessage,
        bool requiresActiveBoard,
        CancellationToken ct)
    {
        var effectiveRoleDto = await GetEffectiveBoardRoleAsync(boardId, ct);

        if (effectiveRoleDto == null)
        {
            throw new ForbiddenException("You are not a member of this board or it does not exist.");
        }

        var effectiveBoardRole = (BoardRole)effectiveRoleDto;

        if (!permissionCheck(effectiveBoardRole))
        {
            throw new ForbiddenException(errorMessage);
        }

        if (requiresActiveBoard)
        {
            var isArchived = await boardRepository.IsBoardArchivedAsync(boardId, ct);

            if (isArchived)
            {
                throw new BusinessRuleValidationException("This action cannot be performed because the board is archived.");
            }
        }

        var (userId, _) = await GetCurrentUserAsync(ct);
        return new BoardAccessContext(userId, effectiveBoardRole);
    }
}
