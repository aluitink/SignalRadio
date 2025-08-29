using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Models;
using SignalRadio.Core.Repositories;
using SignalRadio.Core.Services;
using SignalRadio.Api.Services;
using System.Text.Json;
using System.Threading.Channels;
using System.Collections.Concurrent;

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
    _maxConcurrency = configuration.GetValue<int>("Transcription:MaxConcurrency", 1); // Default to 1
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

        // Producer/consumer setup. We avoid fetching int.MaxValue items up-front.
        var queueCapacity = Math.Max(_maxConcurrency, 1);
        _logger.LogInformation("Starting producer-consumer with max concurrency {MaxConcurrency} and queue capacity {QueueCapacity}", _maxConcurrency, queueCapacity);

        var channel = Channel.CreateBounded<Recording>(new BoundedChannelOptions(queueCapacity)
        {
            SingleWriter = false,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    // Track which recordings we've already enqueued to avoid duplicates (thread-safe)
    var enqueued = new ConcurrentDictionary<long, byte>();

        // Counters/state for coordination
        var processedAny = false;
        var activeWorkers = 0;
        var queuedCount = 0;
        var lastEnqueueUtc = DateTime.UtcNow;

        // Helper to try enqueue a recording without duplicates
        async Task<bool> TryEnqueueAsync(Recording rec)
        {
            // TryAdd returns false if the key already exists — this avoids needing an explicit lock
            if (!enqueued.TryAdd(rec.Id, 0))
                return false;

            await channel.Writer.WriteAsync(rec, cancellationToken);
            Interlocked.Increment(ref queuedCount);
            lastEnqueueUtc = DateTime.UtcNow;
            return true;
        }

        // Producer: keep polling and enqueueing pages sized to queueCapacity until a fetch returns empty
        var producer = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Wait until there's room in the queue before fetching more
                    while (Volatile.Read(ref queuedCount) >= queueCapacity && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(200, cancellationToken);
                    }

                    // Fetch a page of pending recordings sized to the queue capacity
                    var pageSize = queueCapacity;
                    var nextBatch = (await recordingRepository.GetRecordingsNeedingTranscriptionAsync(limit: pageSize)).ToList();

                    // If repository returned nothing, stop producing — we've drained the backlog
                    if (!nextBatch.Any())
                        break;

                    foreach (var rec in nextBatch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        await TryEnqueueAsync(rec);
                    }

                    // Yield to allow workers to make progress before fetching the next page
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            finally
            {
                // Signal completion to consumers
                channel.Writer.TryComplete();
            }
        }, cancellationToken);

        // Start worker tasks equal to max concurrency
        var workers = new List<Task>(_maxConcurrency);
        for (int i = 0; i < _maxConcurrency; i++)
        {
            workers.Add(Task.Run(async () =>
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (!channel.Reader.TryRead(out var recording))
                        continue;

                    // We took an item off the queue
                    Interlocked.Decrement(ref queuedCount);

                    Interlocked.Increment(ref activeWorkers);
                    try
                    {
                        using var taskScope = _serviceProvider.CreateScope();
                        var storageService = taskScope.ServiceProvider.GetRequiredService<IStorageService>();
                        var asrServiceTask = taskScope.ServiceProvider.GetRequiredService<IAsrService>();
                        var recordingRepositoryTask = taskScope.ServiceProvider.GetRequiredService<IRecordingRepository>();
                        var callService = taskScope.ServiceProvider.GetRequiredService<ICallService>();
                        var hubContext = taskScope.ServiceProvider.GetRequiredService<IHubContext<TalkGroupHub>>();
                        var talkGroupRepository = taskScope.ServiceProvider.GetRequiredService<SignalRadio.Core.Repositories.ITalkGroupRepository>();

                        // Reload the recording to ensure fresh context
                        var freshRecording = await recordingRepositoryTask.GetByIdAsync(recording.Id);
                        if (freshRecording != null)
                        {
                            processedAny = true;
                            // Fetch talkgroup info if available
                            SignalRadio.Core.Models.TalkGroup? talkGroup = null;
                            try
                            {
                                talkGroup = await talkGroupRepository.GetByDecimalAsync(freshRecording.Call.TalkgroupId);
                            }
                            catch (Exception tgEx)
                            {
                                _logger.LogDebug(tgEx, "Failed to load talkgroup {TalkgroupId} for logging", freshRecording.Call.TalkgroupId);
                            }

                            await ProcessRecordingTranscription(
                                freshRecording,
                                storageService,
                                asrServiceTask,
                                recordingRepositoryTask,
                                callService,
                                hubContext,
                                talkGroup,
                                cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning("Recording {RecordingId} not found for transcription", recording.Id);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing transcription for recording {RecordingId}", recording.Id);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref activeWorkers);
                    }
                }
            }, cancellationToken));
        }

        // Wait for producer and workers to finish
        await Task.WhenAll(workers.Concat(new[] { producer }));

        return processedAny; // true if any recordings were actually processed
    }

    private async Task ProcessRecordingTranscription(
        Recording recording,
        IStorageService storageService,
        IAsrService asrService,
        IRecordingRepository recordingRepository,
        ICallService callService,
        IHubContext<TalkGroupHub> hubContext,
    SignalRadio.Core.Models.TalkGroup? talkGroup,
    CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting transcription for recording {RecordingId}: {FileName}",
            recording.Id, recording.FileName);

        // Update attempts counter
        recording.TranscriptionAttempts++;
        recording.UpdatedAt = DateTime.UtcNow;

            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            var transcriptionDuration = sw.Elapsed;

            // Log call/talkgroup details and timing
            try
            {
                var call = recording.Call;
                var talkgroupDesc = talkGroup?.Description ?? "(unknown)";
                var talkgroupPriority = talkGroup?.Priority?.ToString() ?? "(none)";
                var callCreatedAt = call?.CreatedAt.ToString("o") ?? "(unknown)";
                var callLength = recording.Duration.HasValue ? recording.Duration.Value.ToString() : "(unknown)";

                _logger.LogInformation("Transcription complete - RecordingId={RecordingId}, CallId={CallId}, Talkgroup={Talkgroup}, TalkgroupPriority={Priority}, CallCreatedAt={CallCreatedAt}, CallLength={CallLength}, TranscriptionTimeMs={Ms}",
                    recording.Id,
                    recording.CallId,
                    talkgroupDesc,
                    talkgroupPriority,
                    callCreatedAt,
                    callLength,
                    transcriptionDuration.TotalMilliseconds);
            }
            catch (Exception logEx)
            {
                _logger.LogDebug(logEx, "Failed to log extended transcription details for recording {RecordingId}", recording.Id);
            }

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
