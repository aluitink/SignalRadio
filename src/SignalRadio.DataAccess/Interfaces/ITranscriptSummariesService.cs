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
}
