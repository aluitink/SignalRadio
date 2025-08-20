using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

public interface ISearchService
{
    /// <summary>
    /// Search recordings using Full Text Search on transcription text
    /// </summary>
    /// <param name="searchTerm">The search term to look for</param>
    /// <param name="talkGroupId">Optional filter by talk group</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="pageNumber">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Search results with metadata</returns>
    Task<SearchResult<Recording>> SearchTranscriptionsAsync(
        string searchTerm,
        string? talkGroupId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Get search suggestions/autocomplete for transcription text
    /// </summary>
    /// <param name="partialTerm">Partial search term</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
    /// <returns>List of suggested search terms</returns>
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialTerm, int maxSuggestions = 10);
}

/// <summary>
/// Search result wrapper with pagination metadata
/// </summary>
/// <typeparam name="T">Type of search result items</typeparam>
public class SearchResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
