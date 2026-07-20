using Application.Common.Models;
using Contracts.Enums;

namespace Application.Interfaces.Services;

public interface IBoardAccessService
{
    Task<(Guid UserId, string Email)> GetCurrentUserAsync(CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanEditBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanDeleteBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanManageColumnsAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanManageCommentsAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanManageAttachmentsAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanExportBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardRoleDto?> GetEffectiveBoardRoleAsync(Guid boardId, CancellationToken ct = default);
}
