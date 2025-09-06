using Microsoft.Extensions.Options;
using SignalRadio.Core.AI.Interfaces;
using SignalRadio.Core.AI.Models;
using SignalRadio.DataAccess.Services;

namespace SignalRadio.Api.Services;

/// <summary>
/// Background service for generating AI summaries of transcriptions
/// </summary>
public class AiSummaryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiSummaryBackgroundService> _logger;
    private readonly AiSummaryOptions _options;
    private readonly int _processingIntervalSeconds;

    public AiSummaryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AiSummaryBackgroundService> logger,
        IOptions<AiSummaryOptions> options,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _processingIntervalSeconds = configuration.GetValue<int>("AiSummary:ProcessingIntervalSeconds", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.AutoSummarize)
        {
            _logger.LogInformation("AI Summary background service disabled - Enabled: {Enabled}, Auto Summarize: {AutoSummarize}",
                _options.Enabled, _options.AutoSummarize);
            return;
        }

        _logger.LogInformation("AI Summary background service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processedAny = false;
            try
            {
                processedAny = await ProcessNextPendingSummary(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI summary processing");
            }

            if (!processedAny)
            {
                await Task.Delay(TimeSpan.FromSeconds(_processingIntervalSeconds), stoppingToken);
            }
        }

        _logger.LogInformation("AI Summary background service stopped");
    }

    private async Task<bool> ProcessNextPendingSummary(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var transcriptionsService = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();
        var aiSummaryService = scope.ServiceProvider.GetRequiredService<IAiSummaryService>();

        if (!await aiSummaryService.IsAvailableAsync())
        {
            _logger.LogWarning("AI Summary service is not available, skipping processing");
            return false;
        }

        // Get the next transcription that needs summarization
        var pending = (await transcriptionsService.GetTranscriptionsNeedingSummaryAsync(limit: 1)).FirstOrDefault();
        if (pending == null)
            return false;

        try
        {
            await ProcessTranscriptionSummary(pending, aiSummaryService, transcriptionsService, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process summary for transcription {Id}", pending.Id);
            
            // Mark the attempt as failed
            try
            {
                await transcriptionsService.UpdateTranscriptionSummaryAsync(
                    pending.Id, 
                    null, 
                    $"Processing failed: {ex.Message}");
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update transcription summary error for {Id}", pending.Id);
            }
            
            return false;
        }
    }

    private async Task ProcessTranscriptionSummary(
        SignalRadio.DataAccess.Transcription transcription,
        IAiSummaryService aiSummaryService,
        ITranscriptionsService transcriptionsService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting AI summarization for transcription {TranscriptionId}", transcription.Id);

        try
        {
            // Build context information
            var context = BuildContextString(transcription);
            
            // Generate the summary
            var summaryResult = await aiSummaryService.SummarizeAsync(
                transcription.FullText, 
                context, 
                cancellationToken);

            // Update the transcription with the summary result
            await transcriptionsService.UpdateTranscriptionSummaryAsync(
                transcription.Id,
                summaryResult);

            if (summaryResult.IsSuccessful)
            {
                _logger.LogInformation("Successfully generated summary for transcription {TranscriptionId} in {ProcessingTimeMs}ms", 
                    transcription.Id, summaryResult.ProcessingTimeMs);
            }
            else
            {
                _logger.LogWarning("Failed to generate summary for transcription {TranscriptionId}: {Error}", 
                    transcription.Id, summaryResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI summarization failed for transcription {TranscriptionId}", transcription.Id);
            throw; // Re-throw to be handled by the caller
        }
    }

    private string BuildContextString(SignalRadio.DataAccess.Transcription transcription)
    {
        var contextParts = new List<string>();
        
        if (transcription.Recording?.Call?.TalkGroup?.Name != null)
        {
            contextParts.Add($"Talk Group: {transcription.Recording.Call.TalkGroup.Name}");
        }
        
        if (transcription.Recording?.Call?.RecordingTime != null)
        {
            contextParts.Add($"Time: {transcription.Recording.Call.RecordingTime:yyyy-MM-dd HH:mm:ss} UTC");
        }
        
        if (transcription.Language != null)
        {
            contextParts.Add($"Language: {transcription.Language}");
        }
        
        if (transcription.Confidence.HasValue)
        {
            contextParts.Add($"Transcription Confidence: {transcription.Confidence:F2}");
        }

        return contextParts.Count > 0 ? string.Join(", ", contextParts) : "";
    }
}