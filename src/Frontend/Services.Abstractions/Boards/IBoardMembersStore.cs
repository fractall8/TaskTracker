using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.Boards;

public interface IBoardMembersStore
{
    IReadOnlyList<BoardMemberDto> Members { get; }
    bool IsLoading { get; }
    bool IsProcessing { get; }
    string? ErrorMessage { get; }

    event Action? StateChanged;

    Task LoadAsync(Guid boardId, CancellationToken ct = default);
    Task AddMemberAsync(Guid boardId, Guid workspaceMemberId, BoardRoleDto role, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid boardId, Guid workspaceMemberId, BoardRoleDto newRole, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid boardId, Guid workspaceMemberId, CancellationToken ct = default);
    void Reset();
}
