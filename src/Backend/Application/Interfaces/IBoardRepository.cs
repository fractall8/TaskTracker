using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IBoardRepository : IRepository<Board, Guid>
{
    Task<Board?> GetBoardWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<bool> HasRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default, params BoardRole[] allowedRoles);
}