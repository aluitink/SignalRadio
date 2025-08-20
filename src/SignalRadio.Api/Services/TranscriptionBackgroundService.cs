using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Models;
using SignalRadio.Core.Repositories;
using SignalRadio.Core.Services;
using System.Text.Json;

namespace SignalRadio.Api.Services;

/// <summary>
/// Background service for processing audio transcriptions
/// </summary>
public class TranscriptionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TranscriptionBackgroundService> _logger;
    private readonly AsrOptions _asrOptions;
    private readonly int _processingIntervalSeconds = 2; // Check for new recordings every 2 seconds

    public TranscriptionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TranscriptionBackgroundService> logger,
        IOptions<AsrOptions> asrOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _asrOptions = asrOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_asrOptions.Enabled || !_asrOptions.AutoTranscribe)
        {
            _logger.LogInformation("ASR background service disabled - ASR Enabled: {Enabled}, Auto Transcribe: {AutoTranscribe}",
                _asrOptions.Enabled, _asrOptions.AutoTranscribe);
            return;
        }

        _logger.LogInformation("ASR background service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool hadPendingRecordings = false;
            
            try
            {
                hadPendingRecordings = await ProcessPendingTranscriptions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during transcription processing");
            }

            // Only delay if there were no pending recordings to process
            if (!hadPendingRecordings)
            {
                await Task.Delay(TimeSpan.FromSeconds(_processingIntervalSeconds), stoppingToken);
            }
        }

        _logger.LogInformation("ASR background service stopped");
    }

    private async Task<bool> ProcessPendingTranscriptions(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var recordingRepository = scope.ServiceProvider.GetRequiredService<IRecordingRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var asrService = scope.ServiceProvider.GetRequiredService<IAsrService>();
        var callService = scope.ServiceProvider.GetRequiredService<ICallService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TalkGroupHub>>();

        // Check if ASR service is available
        if (!await asrService.IsAvailableAsync())
        {
            _logger.LogWarning("ASR service is not available, skipping processing");
            return false;
        }

        // Get recordings that need transcription (WAV files that are uploaded but not transcribed)
        var pendingRecordings = await recordingRepository.GetRecordingsNeedingTranscriptionAsync(limit: 5);

        if (!pendingRecordings.Any())
        {
            _logger.LogDebug("No recordings pending transcription");
            return false;
        }

        _logger.LogInformation("Processing {Count} recordings for transcription", pendingRecordings.Count());

        foreach (var recording in pendingRecordings)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessRecordingTranscription(recording, storageService, asrService, recordingRepository, callService, hubContext, cancellationToken);
        }

        return true; // Had pending recordings to process
    }

    private async Task ProcessRecordingTranscription(
        Recording recording,
        IStorageService storageService,
        IAsrService asrService,
        IRecordingRepository recordingRepository,
        ICallService callService,
        IHubContext<TalkGroupHub> hubContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting transcription for recording {RecordingId}: {FileName}",
            recording.Id, recording.FileName);

        // Update attempts counter
        recording.TranscriptionAttempts++;
        recording.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Download the audio file from storage
            var audioData = await storageService.DownloadFileAsync(recording.BlobName!, cancellationToken);
            if (audioData == null || audioData.Length == 0)
            {
                throw new InvalidOperationException($"Failed to download audio file: {recording.BlobName}");
            }

            // Transcribe the audio
            var transcriptionResult = await asrService.TranscribeAsync(audioData, recording.FileName, cancellationToken);

            // Update the recording with transcription results
            recording.HasTranscription = true;
            recording.TranscriptionText = transcriptionResult.Text;
            recording.TranscriptionLanguage = transcriptionResult.Language;
            recording.TranscriptionProcessedAt = DateTime.UtcNow;
            recording.LastTranscriptionError = null;

            // Store detailed segments as JSON if available
            if (transcriptionResult.Segments?.Any() == true)
            {
                recording.TranscriptionSegments = JsonSerializer.Serialize(transcriptionResult.Segments, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Calculate overall confidence from segments
                var confidenceValues = transcriptionResult.Segments
                    .Where(s => s.Confidence.HasValue)
                    .Select(s => s.Confidence!.Value)
                    .ToList();

                if (confidenceValues.Any())
                {
                    recording.TranscriptionConfidence = confidenceValues.Average();
                }
            }

            await recordingRepository.UpdateAsync(recording);

            _logger.LogInformation("Transcription completed for recording {RecordingId}. Text length: {Length} chars, Language: {Language}",
                recording.Id, transcriptionResult.Text.Length, transcriptionResult.Language);

            // Broadcast the updated call with transcription to connected clients
            try
            {
                var updatedCall = await callService.GetCallByIdAsync(recording.CallId);
                if (updatedCall != null)
                {
                    var callNotification = new
                    {
                        updatedCall.Id,
                        updatedCall.TalkgroupId,
                        updatedCall.SystemName,
                        updatedCall.RecordingTime,
                        updatedCall.Frequency,
                        updatedCall.Duration,
                        updatedCall.CreatedAt,
                        updatedCall.UpdatedAt,
                        RecordingCount = updatedCall.Recordings?.Count ?? 0,
                        Recordings = updatedCall.Recordings?.Select(r => new
                        {
                            r.Id,
                            r.FileName,
                            r.Format,
                            r.FileSize,
                            r.IsUploaded,
                            r.BlobName,
                            r.UploadedAt,
                            r.HasTranscription,
                            r.TranscriptionText,
                            r.TranscriptionLanguage,
                            r.TranscriptionConfidence,
                            r.TranscriptionProcessedAt
                        }) ?? Enumerable.Empty<object>()
                    };

                    // Broadcast to all clients monitoring the general call stream
                    await hubContext.Clients.Group("all_calls_monitor")
                        .SendAsync("CallUpdated", callNotification);

                    // Broadcast to clients subscribed to this specific talk group
                    await hubContext.Clients.Group($"talkgroup_{updatedCall.TalkgroupId}")
                        .SendAsync("CallUpdated", callNotification);

                    _logger.LogDebug("Broadcasted transcription update for call {CallId} to SignalR clients", updatedCall.Id);
                }
            }
            catch (Exception broadcastEx)
            {
                _logger.LogWarning(broadcastEx, "Failed to broadcast transcription update for recording {RecordingId}, but transcription was successful", recording.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for recording {RecordingId}: {FileName}",
                recording.Id, recording.FileName);

            // Store the error details
            recording.LastTranscriptionError = ex.Message;
            recording.HasTranscription = false;
            
            try
            {
                await recordingRepository.UpdateAsync(recording);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update recording {RecordingId} with error details", recording.Id);
            }
        }
    }
}
