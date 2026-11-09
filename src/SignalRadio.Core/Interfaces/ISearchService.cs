using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResultsDto> SearchAsync(string query, int page, int pageSize, bool semantic, bool hybrid, CancellationToken ct = default);
    Task<IReadOnlyList<TranscriptSummaryDto>> SearchSummariesAsync(string query, int count, CancellationToken ct = default);
    Task<IReadOnlyList<NotableIncidentDto>> SearchIncidentsAsync(string query, int count, CancellationToken ct = default);

    /// <summary>
    /// Cursor-paged summary search (newest first). Pass the previous page's
    /// cursor (EndTime + Id of its oldest summary) to fetch the next page.
    /// </summary>
    Task<PagedSummarySearchResult> SearchSummariesPagedAsync(
        string query, int pageSize, DateTimeOffset? beforeTime, long? beforeId, CancellationToken ct = default);
}
