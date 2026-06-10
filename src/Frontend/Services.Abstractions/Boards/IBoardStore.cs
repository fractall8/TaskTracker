using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.Boards;

public interface IBoardStore
{
    int PageSize { get; }
    
    Task ChangePageSizeAsync(int newSize, CancellationToken ct = default);
    
    string? SearchTerm { get; }
    
    Task SetSearchTermAsync(string? searchTerm, CancellationToken ct = default);
    
    IReadOnlyList<BoardPreviewDto> Boards { get; }

    PaginationMetadata Pagination { get; }
    
    int CurrentPage { get; }
    
    bool IsLoading { get; }
    
    bool IsLoaded { get; }
    
    string? ErrorMessage { get; }

    event Action? StateChanged;

    Task LoadAsync(int pageNumber, CancellationToken ct = default);

    Task RefreshAsync(CancellationToken ct = default);

    BoardRoleDto? GetCachedRole(Guid boardId);

    void Reset();
}