using Contracts.DTOs;
using Contracts.Requests.Boards;
using Services.Abstractions.Boards;
using Services.Api;
using Services.Extensions;

namespace Services.Boards;

public class BoardMembersApiService(IBoardMembersApi api) : IBoardMembersApiService
{
    public async Task<List<BoardMemberDto>> GetMembersAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await api.GetMembersAsync(boardId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task AddMemberAsync(Guid boardId, AddBoardMemberRequest request, CancellationToken ct = default)
    {
        var response = await api.AddMemberAsync(boardId, request, ct);
        await response.HandleResponseAsync();
    }

    public async Task UpdateRoleAsync(Guid boardId, Guid workspaceMemberId, UpdateBoardMemberRoleRequest request,
        CancellationToken ct = default)
    {
        var response = await api.UpdateRoleAsync(boardId, workspaceMemberId, request, ct);
        await response.HandleResponseAsync();
    }

    public async Task RemoveMemberAsync(Guid boardId, Guid workspaceMemberId, CancellationToken ct = default)
    {
        var response = await api.RemoveMemberAsync(boardId, workspaceMemberId, ct);
        await response.HandleResponseAsync();
    }
}
