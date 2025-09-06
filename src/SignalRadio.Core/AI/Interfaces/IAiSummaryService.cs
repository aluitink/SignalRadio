using SignalRadio.Core.AI.Models;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.AI.Interfaces;

/// <summary>
/// Service interface for AI-powered transcript summarization operations
/// </summary>
public interface IAiSummaryService
{
    /// <summary>
    /// Generate a summary for a transcript text
    /// </summary>
    /// <param name="transcriptText">The transcript text to summarize</param>
    /// <param name="context">Optional context information (e.g., talkgroup name, time, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary result</returns>
    Task<SummaryResult> SummarizeAsync(string transcriptText, string? context = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate a summary from a transcription result
    /// </summary>
    /// <param name="transcriptionResult">The transcription result to summarize</param>
    /// <param name="context">Optional context information (e.g., talkgroup name, time, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary result</returns>
    Task<SummaryResult> SummarizeAsync(TranscriptionResult transcriptionResult, string? context = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if AI summarization service is enabled and available
    /// </summary>
    /// <returns>True if AI summarization is available</returns>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// Get the health status of the AI summarization service
    /// </summary>
    /// <returns>Health check result</returns>
    Task<AiSummaryHealthStatus> GetHealthStatusAsync();
}

/// <summary>
/// AI summarization service health status
/// </summary>
public enum AiSummaryHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}