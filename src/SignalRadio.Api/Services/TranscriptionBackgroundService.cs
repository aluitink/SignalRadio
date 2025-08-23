using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Models;
using SignalRadio.Core.Repositories;
using SignalRadio.Core.Services;
using SignalRadio.Api.Services;
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
    private readonly int _processingIntervalSeconds; // Delay in seconds when no work (configurable via Transcription:ProcessingIntervalSeconds, default 2)
    private readonly ILocalFileCacheService _fileCacheService;
    private readonly int _maxConcurrency;

    public TranscriptionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TranscriptionBackgroundService> logger,
        IOptions<AsrOptions> asrOptions,
        ILocalFileCacheService fileCacheService,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _asrOptions = asrOptions.Value;
        _fileCacheService = fileCacheService;
    _processingIntervalSeconds = configuration.GetValue<int>("Transcription:ProcessingIntervalSeconds", 2);
    _maxConcurrency = configuration.GetValue<int>("Transcription:MaxConcurrency", 2); // Default to 2
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

            // Clean up expired cache files after each loop
            try
            {
                _fileCacheService.Cleanup();
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Cache cleanup failed");
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
        var asrService = scope.ServiceProvider.GetRequiredService<IAsrService>();

        // Check if ASR service is available
        if (!await asrService.IsAvailableAsync())
        {
            _logger.LogWarning("ASR service is not available, skipping processing");
            return false;
        }

        // Get recordings that need transcription (WAV files that are uploaded but not transcribed)
        var pendingRecordings = await recordingRepository.GetRecordingsNeedingTranscriptionAsync(limit: _maxConcurrency);

        if (!pendingRecordings.Any())
        {
            _logger.LogDebug("No recordings pending transcription");
            return false;
        }

        _logger.LogInformation("Processing {Count} recordings for transcription", pendingRecordings.Count());

        var semaphore = new System.Threading.SemaphoreSlim(_maxConcurrency);
        var tasks = new List<Task>();
        foreach (var recording in pendingRecordings)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await semaphore.WaitAsync(cancellationToken);
            tasks.Add(Task.Run(async () =>
            {
                using var taskScope = _serviceProvider.CreateScope();
                var storageService = taskScope.ServiceProvider.GetRequiredService<IStorageService>();
                var asrServiceTask = taskScope.ServiceProvider.GetRequiredService<IAsrService>();
                var recordingRepositoryTask = taskScope.ServiceProvider.GetRequiredService<IRecordingRepository>();
                var callService = taskScope.ServiceProvider.GetRequiredService<ICallService>();
                var hubContext = taskScope.ServiceProvider.GetRequiredService<IHubContext<TalkGroupHub>>();

                try
                {
                    // Reload the recording from the repository to ensure a fresh context
                    var freshRecording = await recordingRepositoryTask.GetByIdAsync(recording.Id);
                    if (freshRecording != null)
                    {
                        await ProcessRecordingTranscription(
                            freshRecording,
                            storageService,
                            asrServiceTask,
                            recordingRepositoryTask,
                            callService,
                            hubContext,
                            cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("Recording {RecordingId} not found for transcription", recording.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing transcription for recording {recording.Id}");
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);

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
            byte[]? audioData;
            // Try to get file from local cache first
            if (_fileCacheService.TryGetFile(recording.FileName, out var cachedPath))
            {
                audioData = await System.IO.File.ReadAllBytesAsync(cachedPath, cancellationToken);
                _logger.LogInformation("Used cached file for transcription: {FileName}", recording.FileName);
            }
            else
            {
                // Download the audio file from storage and cache it
                audioData = await storageService.DownloadFileAsync(recording.BlobName!, cancellationToken);
                if (audioData == null || audioData.Length == 0)
                {
                    throw new InvalidOperationException($"Failed to download audio file: {recording.BlobName}");
                }
                // Save to cache for future use
                await _fileCacheService.SaveFileAsync(recording.FileName, new System.IO.MemoryStream(audioData));
                _logger.LogInformation("Downloaded and cached file for transcription: {FileName}", recording.FileName);
            }

            // Transcribe the audio
            var transcriptionResult = await asrService.TranscribeAsync(audioData, recording.FileName, cancellationToken);

            // ...existing code...
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

            // Print a summary when we store the transcript
            _logger.LogInformation("Transcript stored: RecordingId={RecordingId}, TextLength={TextLength}, Language={Language}, Confidence={Confidence}",
                recording.Id,
                transcriptionResult.Text?.Length ?? 0,
                transcriptionResult.Language ?? "unknown",
                recording.TranscriptionConfidence.HasValue ? recording.TranscriptionConfidence.Value.ToString("F3") : "null");

            // ...existing code...
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
