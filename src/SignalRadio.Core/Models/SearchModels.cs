namespace SignalRadio.Core.Models;

/// <summary>
/// Represents a full-text search result
/// </summary>
public class SearchResult
{
    public string Type { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public double Relevance { get; set; }
}

/// <summary>
/// Paged result wrapper for search results
/// </summary>
public class SearchResultPage
{
    public IEnumerable<SearchResult> Results { get; set; } = Enumerable.Empty<SearchResult>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => (Page * PageSize) < TotalCount;
    public bool HasPreviousPage => Page > 1;
}
