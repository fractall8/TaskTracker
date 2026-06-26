using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IBoardMemberRepository : IRepository<BoardMember, Guid>
{
    Task<List<BoardMember>> GetByWorkspaceMemberIdAsync(Guid workspaceMemberId, CancellationToken ct = default);

    Task<BoardMember?> GetByBoardAndUserIdAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}
