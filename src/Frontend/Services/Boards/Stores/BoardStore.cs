using Contracts.DTOs;
using Contracts.Enums;
using Services.Abstractions.Boards;

namespace Services.Boards.Stores;

internal sealed class BoardStore(IBoardApiService boardApiService) : IBoardStore
{
    public int PageSize { get; private set; } = 24;

    private readonly Dictionary<Guid, BoardRoleDto> _roleCache = [];

    public string? SearchTerm { get; private set; }
    
    public async Task SetSearchTermAsync(string? searchTerm, CancellationToken ct = default)
    {
        if (SearchTerm == searchTerm) 
            return;

        SearchTerm = searchTerm;
        CurrentPage = 1;
        await LoadInternalAsync(CurrentPage, ct);
    }

    public IReadOnlyList<BoardPreviewDto> Boards { get; private set; } = [];
    
    public PaginationMetadata Pagination { get; private set; } = new();
    
    public int CurrentPage { get; private set; } = 1;
    
    public bool IsLoading { get; private set; }
    
    public bool IsLoaded { get; private set; }
    
      public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;
    
    public Task ChangePageSizeAsync(int newSize, CancellationToken ct = default)
    {
        if (PageSize == newSize) 
            return Task.CompletedTask;

        PageSize = newSize;
        CurrentPage = 1;
        
        return LoadInternalAsync(CurrentPage, ct);
    }

    public Task LoadAsync(int pageNumber, CancellationToken ct = default)
    {
        if (IsLoaded && pageNumber == CurrentPage && !IsLoading)
            return Task.CompletedTask;

        return LoadInternalAsync(pageNumber, ct);
    }

    public Task RefreshAsync(CancellationToken ct = default) =>
        LoadInternalAsync(CurrentPage, ct);

    public BoardRoleDto? GetCachedRole(Guid boardId) =>
        _roleCache.TryGetValue(boardId, out var role) ? role : null;

    public void Reset()
    {
        Boards = [];
        Pagination = new PaginationMetadata();
        CurrentPage = 1;
        IsLoading = false;
        IsLoaded = false;
        ErrorMessage = null;
        _roleCache.Clear();
        NotifyStateChanged();
    }

    private async Task LoadInternalAsync(int pageNumber, CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var page = await boardApiService.GetMyBoardsAsync(pageNumber, PageSize, SearchTerm);

            Boards = page.Items;
            Pagination = page.Metadata;
            CurrentPage = page.Metadata.CurrentPage;
            IsLoaded = true;

            foreach (var board in page.Items)
                _roleCache[board.Id] = board.Role;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}