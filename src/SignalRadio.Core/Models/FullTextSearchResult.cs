namespace SignalRadio.Core.Models;

/// <summary>
/// Represents a full-text search result across different content types
/// </summary>
public class FullTextSearchResult
{
    public string Type { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public double Relevance { get; set; }
}
