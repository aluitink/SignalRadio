namespace SignalRadio.Core.AI.Models;

/// <summary>
/// Configuration options for AI-powered transcript summarization
/// </summary>
public class AiSummaryOptions
{
    public const string SectionName = "AiSummary";
    
    /// <summary>
    /// Whether AI summarization is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Whether to automatically generate summaries for new transcriptions
    /// </summary>
    public bool AutoSummarize { get; set; } = true;
    
    /// <summary>
    /// Azure OpenAI endpoint URL
    /// </summary>
    public string? AzureOpenAiEndpoint { get; set; }
    
    /// <summary>
    /// Azure OpenAI API key (can also be supplied via AZURE_OPENAI_API_KEY env var)
    /// </summary>
    public string? AzureOpenAiApiKey { get; set; }
    
    /// <summary>
    /// The deployment name of the model to use for summarization
    /// </summary>
    public string ModelDeployment { get; set; } = "gpt-35-turbo";
    
    /// <summary>
    /// Maximum number of tokens to use for the summary
    /// </summary>
    public int MaxTokens { get; set; } = 150;
    
    /// <summary>
    /// Temperature for AI generation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;
    
    /// <summary>
    /// Timeout for AI requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Minimum transcript length required for summarization
    /// </summary>
    public int MinTranscriptLength { get; set; } = 50;
}