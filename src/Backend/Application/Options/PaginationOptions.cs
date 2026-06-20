namespace Application.Options;

public class PaginationOptions
{
    public const string SectionName = "Pagination";

    public int DefaultPageSize { get; set; } = 24;

    public int MaxPageSize { get; set; } = 100;

    public int MaxSearchTermLength { get; set; } = 100;
}
