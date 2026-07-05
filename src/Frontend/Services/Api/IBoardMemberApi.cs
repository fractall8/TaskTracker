using Contracts.DTOs;
using Contracts.Requests.Boards;
using Refit;

namespace Services.Api;

public interface IBoardMembersApi
{
    [Get("/api/boards/{boardId}/members")]
    Task<IApiResponse<List<BoardMemberDto>>> GetMembersAsync(Guid boardId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/members")]
    Task<IApiResponse> AddMemberAsync(Guid boardId, [Body] AddBoardMemberRequest request, CancellationToken ct = default);

    [Put("/api/boards/{boardId}/members/{workspaceMemberId}")]
    Task<IApiResponse> UpdateRoleAsync(Guid boardId, Guid workspaceMemberId, [Body] UpdateBoardMemberRoleRequest request, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/members/{workspaceMemberId}")]
    Task<IApiResponse> RemoveMemberAsync(Guid boardId, Guid workspaceMemberId, CancellationToken ct = default);
}
