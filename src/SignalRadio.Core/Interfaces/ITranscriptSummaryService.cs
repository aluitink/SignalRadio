using SignalRadio.Core.Models;

namespace SignalRadio.Core.Interfaces;

public interface ITranscriptSummaryService
{
    /// <summary>
    /// Generate a summary of transcripts for a specific talkgroup within a time window
    /// </summary>
    /// <param name="request">Summary request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary response with AI-generated analysis</returns>
    Task<TranscriptSummaryResponse?> GenerateSummaryAsync(TranscriptSummaryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the service is available and properly configured
    /// </summary>
    /// <returns>True if the service is available</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Clear cached summaries for a specific talkgroup or all cached summaries
    /// </summary>
    /// <param name="talkGroupId">Optional talkgroup ID to clear cache for (null = clear all)</param>
    Task ClearCacheAsync(int? talkGroupId = null);
}
