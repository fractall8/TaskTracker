using Application.Common.Models;

namespace Application.Interfaces.Services;

public interface IBoardAccessService
{
    Task<(Guid UserId, string Email)> GetCurrentUserAsync(CancellationToken ct = default);

    Task<BoardAccessContext> EnsureCanEditBoardAsync(Guid boardId, CancellationToken ct = default);
    
    Task<BoardAccessContext> EnsureCanDeleteBoardAsync(Guid boardId, CancellationToken ct = default);
    
    Task<BoardAccessContext> EnsureCanManageColumnsAsync(Guid boardId, CancellationToken ct = default);
    
    Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default);
    
    Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid boardId, CancellationToken ct = default);
}