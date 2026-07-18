namespace CanvasArt.API.Models.Common;

/// <summary>Base query parameters for paginated, searchable, sortable list endpoints.</summary>
public abstract class PagedQuery
{
    private const int MaxPageSize = 100;
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>1-based page index.</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>Page size, clamped to the range [1, 100].</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>Free-text search term.</summary>
    public string? Search { get; set; }

    /// <summary>Column key to sort by (whitelisted by each repository).</summary>
    public string? SortBy { get; set; }

    /// <summary>Sort direction: <c>asc</c> or <c>desc</c>.</summary>
    public string? SortDir { get; set; }

    public int Offset => (Page - 1) * PageSize;

    public bool IsDescending =>
        string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}
