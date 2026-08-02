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

    /// <summary>
    /// Maximum number of concurrent AI requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 3;

    /// <summary>
    /// Minimum time between duplicate requests in minutes
    /// </summary>
    public int MinRequestIntervalMinutes { get; set; } = 2;

    /// <summary>
    /// Minimum transcript content length to trigger AI processing
    /// </summary>
    public int MinTranscriptLength { get; set; } = 50;

    /// <summary>
    /// Base URL for an OpenAI-compatible endpoint (e.g. http://localhost:8000/v1 for vLLM).
    /// When set and AzureOpenAIEndpoint is not configured, this endpoint is used instead.
    /// </summary>
    public string OpenAICompatibleEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// API key for the OpenAI-compatible endpoint. Use any non-empty string for local
    /// deployments that do not require authentication (e.g. "none").
    /// </summary>
    public string OpenAICompatibleApiKey { get; set; } = "none";

    /// <summary>
    /// Model/deployment name to request from the OpenAI-compatible endpoint.
    /// For vLLM this is the model name passed to --model at startup (e.g. "meta-llama/Llama-3-8b-instruct").
    /// </summary>
    public string OpenAICompatibleModelName { get; set; } = string.Empty;
}
