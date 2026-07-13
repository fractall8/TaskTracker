using Contracts.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface IBoardRepository : IRepository<Board, Guid>
{
    Task<int> CountMemberWorkspaceBoardsAsync(Guid workspaceId, Guid userId, string? searchTerm = null, CancellationToken ct = default);

    Task<int> CountArchivedMemberWorkspaceBoardsAsync(Guid workspaceId, Guid userId, string? searchTerm = null,
        CancellationToken ct = default);

    Task<List<Board>> GetMemberWorkspaceBoardsPaginatedAsync(Guid workspaceId, Guid userId, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<List<Board>> GetMyArchivedWorkspaceBoardsAsync(
        Guid workspaceId,
        Guid userId,
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken ct = default);

    Task<int> CountAllWorkspaceBoardsAsync(Guid workspaceId, string? searchTerm = null, CancellationToken ct = default);

    Task<List<Board>> GetAllWorkspaceBoardsPaginatedAsync(Guid workspaceId, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<Board?> GetBoardWithHierarchyAsync(Guid boardId, string? searchTerm = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default);

    Task<int> CountUserBoardsAsync(Guid userId, string? searchTerm = null, CancellationToken ct = default);

    Task<List<Board>> GetUserBoardsPaginatedAsync(Guid userId, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<BoardRole?> GetUserRoleAsync(Guid boardId, Guid userId, CancellationToken ct = default);

    Task<List<Board>> GetBoardsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);

    Task<int> CountBoardsByWorkspaceIdAsync(Guid workspaceId, Guid userId, string? searchTerm = null, CancellationToken ct = default);

    Task<List<Board>> GetBoardsByWorkspaceIdPaginatedAsync(Guid workspaceId, Guid userId, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<List<BoardMemberDto>> GetBoardMembersAsync(Guid boardId, CancellationToken ct = default);

    Task SoftDeleteCascadeAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardExportDataDto?> GetBoardExportDataAsync(Guid boardId, BoardExportOptionsDto options,
        CancellationToken ct = default);
}
