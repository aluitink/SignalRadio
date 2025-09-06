using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SignalRadio.Core.AI.Interfaces;
using SignalRadio.Core.AI.Models;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.AI.Services;

/// <summary>
/// AI summarization service implementation using Azure OpenAI via Semantic Kernel
/// </summary>
public class AzureOpenAiSummaryService : IAiSummaryService
{
    private readonly AiSummaryOptions _options;
    private readonly ILogger<AzureOpenAiSummaryService>? _logger;
    private readonly Kernel? _kernel;

    public AzureOpenAiSummaryService(IOptions<AiSummaryOptions> options, ILogger<AzureOpenAiSummaryService>? logger = null)
    {
        _options = options?.Value ?? new AiSummaryOptions();
        _logger = logger;

        // Initialize Semantic Kernel with Azure OpenAI
        if (!string.IsNullOrWhiteSpace(_options.AzureOpenAiEndpoint) && 
            !string.IsNullOrWhiteSpace(_options.AzureOpenAiApiKey))
        {
            try
            {
                var builder = Kernel.CreateBuilder();
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: _options.ModelDeployment,
                    endpoint: _options.AzureOpenAiEndpoint,
                    apiKey: _options.AzureOpenAiApiKey);
                
                _kernel = builder.Build();
                _logger?.LogInformation("Azure OpenAI Semantic Kernel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to initialize Azure OpenAI Semantic Kernel");
                _kernel = null;
            }
        }
        else
        {
            _logger?.LogWarning("Azure OpenAI configuration incomplete. Set AiSummary:AzureOpenAiEndpoint and AiSummary:AzureOpenAiApiKey in configuration.");
        }
    }

    public async Task<SummaryResult> SummarizeAsync(string transcriptText, string? context = null, CancellationToken cancellationToken = default)
    {
        if (_kernel == null)
        {
            return new SummaryResult
            {
                IsSuccessful = false,
                ErrorMessage = "Azure OpenAI not configured or initialization failed"
            };
        }

        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            return new SummaryResult
            {
                IsSuccessful = false,
                ErrorMessage = "Transcript text is empty"
            };
        }

        if (transcriptText.Length < _options.MinTranscriptLength)
        {
            return new SummaryResult
            {
                Summary = transcriptText,
                IsSuccessful = true,
                Model = _options.ModelDeployment,
                ProcessingTimeMs = 0
            };
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Create the prompt for summarization
            var prompt = CreateSummarizationPrompt(transcriptText, context);
            
            _logger?.LogDebug("Generating summary for transcript of length {Length}", transcriptText.Length);
            
            // Use Semantic Kernel to generate the summary
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);

            stopwatch.Stop();

            var summary = result.ToString().Trim();

            return new SummaryResult
            {
                Summary = summary,
                Model = _options.ModelDeployment,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "Failed to generate summary for transcript");
            
            return new SummaryResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Model = _options.ModelDeployment,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<SummaryResult> SummarizeAsync(TranscriptionResult transcriptionResult, string? context = null, CancellationToken cancellationToken = default)
    {
        if (transcriptionResult == null || string.IsNullOrWhiteSpace(transcriptionResult.Text))
        {
            return new SummaryResult
            {
                IsSuccessful = false,
                ErrorMessage = "Transcription result is null or has no text"
            };
        }

        return await SummarizeAsync(transcriptionResult.Text, context, cancellationToken);
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_options.Enabled && _kernel != null);
    }

    public async Task<AiSummaryHealthStatus> GetHealthStatusAsync()
    {
        if (!_options.Enabled)
            return AiSummaryHealthStatus.Unhealthy;
            
        if (_kernel == null)
            return AiSummaryHealthStatus.Unhealthy;

        try
        {
            // Test with a simple prompt to verify the service is working
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var testResult = await _kernel.InvokePromptAsync("Say 'OK' if you can respond.", cancellationToken: cts.Token);
            var response = testResult.ToString().Trim();
            
            return response.Contains("OK", StringComparison.OrdinalIgnoreCase) 
                ? AiSummaryHealthStatus.Healthy 
                : AiSummaryHealthStatus.Degraded;
        }
        catch
        {
            return AiSummaryHealthStatus.Unhealthy;
        }
    }

    private string CreateSummarizationPrompt(string transcriptText, string? context)
    {
        var contextInfo = !string.IsNullOrWhiteSpace(context) ? $"\n\nContext: {context}" : "";
        
        return $"""
            You are an AI assistant that creates concise summaries of radio communications transcripts. 
            
            Please provide a brief, clear summary of the following radio transcript in 1-2 sentences. 
            Focus on the key information, actions, or events discussed. 
            If the transcript contains emergency or urgent communications, highlight that importance.
            If the transcript is unclear or contains mostly noise/unclear speech, indicate that in your summary.
            {contextInfo}
            
            Radio Transcript:
            {transcriptText}
            
            Summary:
            """;
    }
}