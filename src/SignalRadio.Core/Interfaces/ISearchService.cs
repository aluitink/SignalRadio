using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResultsDto> SearchAsync(string query, int page, int pageSize, bool semantic, CancellationToken ct = default);
    Task<IReadOnlyList<TranscriptSummaryDto>> SearchSummariesAsync(string query, int count, CancellationToken ct = default);
    Task<IReadOnlyList<NotableIncidentDto>> SearchIncidentsAsync(string query, int count, CancellationToken ct = default);
}
