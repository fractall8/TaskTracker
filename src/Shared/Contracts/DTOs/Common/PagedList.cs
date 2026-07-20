namespace Contracts.DTOs;

public class PagedList<T>
{
    public PaginationMetadata Metadata { get; init; } = new();
    public IReadOnlyList<T> Items { get; init; } = [];
}

public class PaginationMetadata
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => CurrentPage > 1;

    public bool HasNext => CurrentPage < TotalPages;
}
