using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface ITranscriptSummariesService
{
    /// <summary>
    /// Get transcript summaries for a specific talkgroup within a time range
    /// </summary>
    Task<IEnumerable<TranscriptSummary>> GetByTalkGroupAndTimeRangeAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// Get a specific transcript summary by ID with all related data
    /// </summary>
    Task<TranscriptSummary?> GetByIdAsync(int id);

    /// <summary>
    /// Create a new transcript summary with topics and notable calls
    /// </summary>
    Task<TranscriptSummary> CreateAsync(TranscriptSummary summary);

    /// <summary>
    /// Update an existing transcript summary
    /// </summary>
    Task<TranscriptSummary> UpdateAsync(TranscriptSummary summary);

    /// <summary>
    /// Delete a transcript summary and all related data
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Check if a summary already exists for the given criteria (for cache lookups)
    /// </summary>
    Task<TranscriptSummary?> FindExistingSummaryAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// Get all transcript summaries with pagination
    /// </summary>
    Task<PagedResult<TranscriptSummary>> GetAllAsync(int page, int pageSize);

    /// <summary>
    /// Get summaries for a specific talkgroup with pagination
    /// </summary>
    Task<PagedResult<TranscriptSummary>> GetByTalkGroupAsync(int talkGroupId, int page, int pageSize);

    /// <summary>
    /// Search topics across all summaries
    /// </summary>
    Task<IEnumerable<TranscriptSummaryTopic>> SearchTopicsAsync(string searchTerm);

    /// <summary>
    /// Get notable calls for a specific call ID
    /// </summary>
    Task<IEnumerable<NotableIncidentCall>> GetNotableCallsByCallIdAsync(int callId);

    /// <summary>
    /// Performs full-text search across summaries, incidents, and topics
    /// </summary>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="contentTypes">Optional filter for specific content types (Summary, Incident, Topic)</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Paged search results</returns>
    Task<SearchResultPage> SearchAsync(string searchTerm, IEnumerable<string>? contentTypes = null, int page = 1, int pageSize = 50);

    /// <summary>
    /// Searches specifically within transcript summaries using full-text search
    /// </summary>
    Task<IEnumerable<TranscriptSummary>> SearchSummariesAsync(string searchTerm, int? talkGroupId = null, 
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int maxResults = 50);

    /// <summary>
    /// Searches specifically within notable incidents using full-text search
    /// </summary>
    Task<IEnumerable<NotableIncident>> SearchIncidentsAsync(string searchTerm, double? minImportanceScore = null, int maxResults = 50);

    /// <summary>
    /// Searches specifically within topics using full-text search
    /// </summary>
    Task<IEnumerable<Topic>> SearchTopicsAsync(string searchTerm, string? category = null, int maxResults = 50);

    /// <summary>
    /// Find a similar summary within a time tolerance to help with caching
    /// </summary>
    Task<TranscriptSummary?> FindSimilarSummaryAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime, int toleranceMinutes = 10);
}
