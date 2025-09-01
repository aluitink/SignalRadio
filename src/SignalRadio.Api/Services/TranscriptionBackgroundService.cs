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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TranscriptionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TranscriptionBackgroundService> logger,
        IOptions<AsrOptions> asrOptions,
        ILocalFileCacheService fileCacheService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    _asrOptions = asrOptions.Value;
    _fileCacheService = fileCacheService;
    _httpClientFactory = httpClientFactory;
    _configuration = configuration;
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
            var transcriptionsService = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();
            var callNotifier = scope.ServiceProvider.GetService<SignalRadio.Core.Services.ICallNotifier>();

            await ProcessRecordingTranscription(
                fresh,
                storageService,
                asrService,
                recordingService,
                callsService,
                transcriptionsService,
                callNotifier,
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
        SignalRadio.Core.Services.ICallNotifier? callNotifier,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("(serial) Starting transcription for recording {RecordingId}: {FileName}", recording.Id, recording.FileName);

        // Track attempt/time
        // Note: DataAccess Recording has different fields than Core; update via recordingService.UpdateAsync
        try
        {
            byte[]? audioData = null;
            if (_fileCacheService.TryGetFile(recording.FileName, out var cachedPath))
            {
                audioData = await System.IO.File.ReadAllBytesAsync(cachedPath, cancellationToken);
            }
            else
            {
                // Prefer calling the API download endpoint so we centralize access rules/urls.
                // API base can be configured via "Api:BaseUrl" config; default to local server root.
                var apiBase = _configuration.GetValue<string>("Api:BaseUrl") ?? string.Empty;
                if (!string.IsNullOrEmpty(apiBase))
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        // Ensure apiBase doesn't double up slashes
                        var sep = apiBase.EndsWith("/") ? string.Empty : "/";
                        var url = apiBase + sep + $"api/recordings/{recording.Id}/file";
                        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                        if (resp.IsSuccessStatusCode)
                        {
                            var ms = new System.IO.MemoryStream();
                            await resp.Content.CopyToAsync(ms, cancellationToken);
                            audioData = ms.ToArray();
                            await _fileCacheService.SaveFileAsync(recording.FileName, new System.IO.MemoryStream(audioData));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "API download failed for recording {RecordingId}, falling back to direct storage", recording.Id);
                    }
                }

                // If audioData still null/empty, fallback to direct storage service call
                if (audioData == null || audioData.Length == 0)
                {
                    audioData = await storageService.DownloadFileAsync(recording.FileName, cancellationToken);
                    if (audioData == null || audioData.Length == 0) throw new InvalidOperationException("Failed to download audio file");
                    await _fileCacheService.SaveFileAsync(recording.FileName, new System.IO.MemoryStream(audioData));
                }
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

            // Notify subscribers about the updated call (best-effort)
            try
            {
                if (callNotifier != null && recording.CallId != 0)
                {
                    await callNotifier.NotifyCallUpdatedAsync(recording.CallId, cancellationToken);
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
