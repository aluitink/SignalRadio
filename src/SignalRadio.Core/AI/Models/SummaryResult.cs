namespace SignalRadio.Core.AI.Models;

/// <summary>
/// Represents the result of an AI summarization operation
/// </summary>
public class SummaryResult
{
    /// <summary>
    /// The generated summary text
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score from the AI model (0.0 to 1.0) if available
    /// </summary>
    public double? Confidence { get; set; }
    
    /// <summary>
    /// The model used for summarization
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of tokens used in the completion
    /// </summary>
    public int? TokensUsed { get; set; }
    
    /// <summary>
    /// Time taken for the summarization in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Whether the summarization was successful
    /// </summary>
    public bool IsSuccessful { get; set; } = true;
    
    /// <summary>
    /// Error message if the summarization failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}