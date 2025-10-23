namespace SignalRadio.Core.Models;

/// <summary>
/// Represents the response from the Whisper ASR service
/// </summary>
public class TranscriptionResult
{
    /// <summary>
    /// The full transcript text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Detected or specified language code (e.g., "en")
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Individual segments with timestamps and confidence
    /// </summary>
    public List<TranscriptionSegment> Segments { get; set; } = new();
}

/// <summary>
/// Represents a segment of the transcription with timing information
/// </summary>
public class TranscriptionSegment
{
    /// <summary>
    /// Segment ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Start time in seconds
    /// </summary>
    public double Start { get; set; }

    /// <summary>
    /// End time in seconds
    /// </summary>
    public double End { get; set; }

    /// <summary>
    /// Transcribed text for this segment
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double? Confidence { get; set; }

    /// <summary>
    /// Average log probability
    /// </summary>
    public double? AvgLogprob { get; set; }

    /// <summary>
    /// Compression ratio
    /// </summary>
    public double? CompressionRatio { get; set; }

    /// <summary>
    /// No speech probability
    /// </summary>
    public double? NoSpeechProb { get; set; }
}
