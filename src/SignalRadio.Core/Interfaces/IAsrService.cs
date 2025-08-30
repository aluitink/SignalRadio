using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

/// <summary>
/// Service interface for Automatic Speech Recognition operations
/// </summary>
public interface IAsrService
{
    /// <summary>
    /// Transcribe an audio file to text
    /// </summary>
    /// <param name="audioData">The audio file data</param>
    /// <param name="fileName">The original filename (for format detection)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result</returns>
    Task<TranscriptionResult> TranscribeAsync(byte[] audioData, string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transcribe an audio stream to text
    /// </summary>
    /// <param name="audioStream">The audio stream</param>
    /// <param name="fileName">The original filename (for format detection)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result</returns>
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if ASR service is enabled and available
    /// </summary>
    /// <returns>True if ASR is available</returns>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// Get the health status of the ASR service
    /// </summary>
    /// <returns>Health check result</returns>
    Task<AsrHealthStatus> GetHealthStatusAsync();
}

/// <summary>
/// ASR service health status
/// </summary>
public enum AsrHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
