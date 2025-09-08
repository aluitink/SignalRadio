namespace SignalRadio.Core.Models;

public class SemanticKernelOptions
{
    public const string SectionName = "SemanticKernel";

    /// <summary>
    /// Whether the Semantic Kernel service is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Azure OpenAI endpoint URL
    /// </summary>
    public string AzureOpenAIEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    public string AzureOpenAIKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI deployment name for the chat model
    /// </summary>
    public string ChatDeploymentName { get; set; } = "gpt-4";

    /// <summary>
    /// Maximum number of tokens for the response
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature for the chat completion (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Cache duration for summaries in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Default time window for summaries in minutes
    /// </summary>
    public int DefaultTimeWindowMinutes { get; set; } = 60;
}
