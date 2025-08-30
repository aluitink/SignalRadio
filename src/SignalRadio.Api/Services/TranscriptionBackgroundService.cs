using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Models;
using SignalRadio.Core.Services;
using SignalRadio.DataAccess.Services;
using System.Text.Json;

namespace SignalRadio.Api.Services;

/// <summary>
/// Simpler, serial transcription background service. Processes the single highest-priority
/// pending recording each loop (by talkgroup priority then call/recording date/time).
/// </summary>
public class TranscriptionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TranscriptionBackgroundService> _logger;
    private readonly AsrOptions _asrOptions;
    private readonly ILocalFileCacheService _fileCacheService;
    private readonly int _processingIntervalSeconds;

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
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_asrOptions.Enabled || !_asrOptions.AutoTranscribe)
        {
            _logger.LogInformation("ASR background service disabled - ASR Enabled: {Enabled}, Auto Transcribe: {AutoTranscribe}",
                _asrOptions.Enabled, _asrOptions.AutoTranscribe);
            return;
        }

        _logger.LogInformation("ASR background service (serial) starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processedAny = false;
            try
            {
                processedAny = await ProcessNextPendingTranscription(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in serial transcription processing");
            }

            // cleanup cache
            try { _fileCacheService.Cleanup(); } catch (Exception) { }

            if (!processedAny)
            {
                await Task.Delay(TimeSpan.FromSeconds(_processingIntervalSeconds), stoppingToken);
            }
        }

        _logger.LogInformation("ASR background service (serial) stopped");
    }

    private async Task<bool> ProcessNextPendingTranscription(CancellationToken cancellationToken)
    {
    using var scope = _serviceProvider.CreateScope();
    var recordingService = scope.ServiceProvider.GetRequiredService<IRecordingsService>();
        var asrService = scope.ServiceProvider.GetRequiredService<IAsrService>();

        if (!await asrService.IsAvailableAsync())
        {
            _logger.LogWarning("ASR service is not available, skipping processing");
            return false;
        }

        // fetch the top recording to process (limit 1)
    var pending = (await recordingService.GetRecordingsNeedingTranscriptionAsync(limit: 1)).FirstOrDefault();
        if (pending == null)
            return false;

        try
        {
            // refresh the recording
            var fresh = await recordingService.GetByIdAsync(pending.Id);
            if (fresh == null)
            {
                _logger.LogWarning("Pending recording disappeared: {Id}", pending.Id);
                return false;
            }

            // resolve dependencies for processing
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var callsService = scope.ServiceProvider.GetRequiredService<ICallsService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TalkGroupHub>>();
            var transcriptionsService = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();

            await ProcessRecordingTranscription(
                fresh,
                storageService,
                asrService,
                recordingService,
                callsService,
                transcriptionsService,
                hubContext,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process pending recording {Id}", pending.Id);
            return false;
        }
    }

    // Reuse the processing logic from the original service but keep it local and simple
    private async Task ProcessRecordingTranscription(
        SignalRadio.DataAccess.Recording recording,
        IStorageService storageService,
        IAsrService asrService,
        IRecordingsService recordingService,
        ICallsService callsService,
        ITranscriptionsService transcriptionsService,
        IHubContext<TalkGroupHub> hubContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("(serial) Starting transcription for recording {RecordingId}: {FileName}", recording.Id, recording.FileName);

        // Track attempt/time
        // Note: DataAccess Recording has different fields than Core; update via recordingService.UpdateAsync
        try
        {
            byte[]? audioData;
            if (_fileCacheService.TryGetFile(recording.FileName, out var cachedPath))
            {
                audioData = await System.IO.File.ReadAllBytesAsync(cachedPath, cancellationToken);
            }
            else
            {
                // DataAccess Recording stores storage location info separately; try downloading by file name (blob name may equal file name)
                audioData = await storageService.DownloadFileAsync(recording.FileName, cancellationToken);
                if (audioData == null || audioData.Length == 0) throw new InvalidOperationException("Failed to download audio file");
                await _fileCacheService.SaveFileAsync(recording.FileName, new System.IO.MemoryStream(audioData));
            }

            var transcriptionResult = await asrService.TranscribeAsync(audioData, recording.FileName, cancellationToken);

            // create Transcription record in DataAccess
            var confidenceValues = transcriptionResult.Segments?.Where(s => s.Confidence.HasValue).Select(s => s.Confidence!.Value).ToList();
            double? overallConfidence = null;
            if (confidenceValues != null && confidenceValues.Any()) overallConfidence = confidenceValues.Average();

            var transcription = new SignalRadio.DataAccess.Transcription
            {
                RecordingId = recording.Id,
                Service = asrService.GetType().Name, // Use the ASR service type
                Language = transcriptionResult.Language,
                FullText = transcriptionResult.Text ?? string.Empty,
                Confidence = overallConfidence,
                AdditionalDataJson = JsonSerializer.Serialize(transcriptionResult.Segments ?? new List<SignalRadio.Core.Models.TranscriptionSegment>()),
                CreatedAt = DateTime.UtcNow,
                IsFinal = true
            };

            await transcriptionsService.CreateAsync(transcription);

            // mark recording as processed (approximate fields)
            recording.IsProcessed = true;
            await recordingService.UpdateAsync(recording.Id, recording);

            // Broadcast update using CallsService
            try
            {
                var updatedCall = await callsService.GetByIdAsync(recording.CallId);
                if (updatedCall != null)
                {
                        var callNotification = new
                        {
                            updatedCall.Id,
                            TalkGroupNumber = updatedCall.TalkGroupId,
                            updatedCall.RecordingTime,
                            FrequencyHz = updatedCall.FrequencyHz,
                            DurationSeconds = updatedCall.DurationSeconds,
                            updatedCall.CreatedAt,
                            RecordingCount = updatedCall.Recordings?.Count ?? 0,
                            Recordings = updatedCall.Recordings?.Select(r => new
                            {
                                r.Id,
                                r.FileName,
                                r.SizeBytes,
                                ReceivedAt = r.ReceivedAt,
                                r.IsProcessed
                            }) ?? Enumerable.Empty<object>()
                        };

                    await hubContext.Clients.Group("all_calls_monitor").SendAsync("CallUpdated", callNotification);
                    await hubContext.Clients.Group($"talkgroup_{updatedCall.TalkGroupId}").SendAsync("CallUpdated", callNotification);
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for recording {RecordingId}: {FileName}", recording.Id, recording.FileName);
            // best effort: increment attempt and persist
            try
            {
                recording.IsProcessed = false;
                await recordingService.UpdateAsync(recording.Id, recording);
            }
            catch { }
        }
    }
}
