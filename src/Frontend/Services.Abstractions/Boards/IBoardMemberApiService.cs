using Contracts.DTOs;
using Contracts.Requests.Boards;

namespace Services.Abstractions.Boards;

public interface IBoardMembersApiService
{
    Task<List<BoardMemberDto>> GetMembersAsync(Guid boardId, CancellationToken ct = default);
    Task AddMemberAsync(Guid boardId, AddBoardMemberRequest request, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid boardId, Guid workspaceMemberId, UpdateBoardMemberRoleRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid boardId, Guid workspaceMemberId, CancellationToken ct = default);
}
