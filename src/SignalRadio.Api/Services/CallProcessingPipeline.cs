using Microsoft.EntityFrameworkCore;
using SignalRadio.Api.Features;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;

namespace SignalRadio.Api.Services;

/// <summary>
/// Inbound call-processing pipeline. For each unprocessed recording it runs ASR to produce a
/// transcription, and (when the Embeddings feature is on) generates + stores a vector embedding.
///
/// Priority ordering:
///   1. Fast path — process the most recent unprocessed recordings first (small batch, short poll),
///      so fresh calls are transcribed as fast as possible.
///   2. Backfill — once the recent backlog is drained, slowly process older recordings (larger
///      lookback, longer poll) so a burst of calls doesn't starve the queue.
///
/// The database is the queue: a recording is "in the queue" until <c>MarkProcessedAsync</c>.
/// A <see cref="SemaphoreSlim"/> bounds in-flight ASR/embed work so a flood of calls can't
/// overwhelm the ASR backend.
///
/// Gated by <see cref="FeatureFlags.Asr"/>. When off, the service idles (no processing, no
/// embeddings — there is no text to embed).
/// </summary>
public class CallProcessingPipeline : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAsrService _asr;
    private readonly ICallNotifier _notifier;
    private readonly FeatureFlags _features;
    private readonly PipelineOptions _options;
    private readonly ILogger<CallProcessingPipeline> _logger;
    private readonly SemaphoreSlim _gate;

    public CallProcessingPipeline(
        IServiceScopeFactory scopeFactory,
        IAsrService asr,
        ICallNotifier notifier,
        FeatureFlags features,
        PipelineOptions options,
        ILogger<CallProcessingPipeline> logger)
    {
        _scopeFactory = scopeFactory;
        _asr = asr;
        _notifier = notifier;
        _features = features;
        _options = options;
        _logger = logger;
        _gate = new SemaphoreSlim(Math.Max(1, options.MaxConcurrent));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_features.Asr)
        {
            _logger.LogInformation("Call processing pipeline disabled (Features:Asr=false). Idling.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation(
            "Call processing pipeline started (embeddings={Embeddings}, maxConcurrent={Max}).",
            _features.Embeddings, _options.MaxConcurrent);

        while (!stoppingToken.IsCancellationRequested)
        {
            var didRecentWork = false;
            try
            {
                didRecentWork = await ProcessRecentAsync(stoppingToken);
                if (!didRecentWork)
                {
                    await ProcessBackfillAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in call processing pipeline.");
            }

            var delay = didRecentWork
                ? TimeSpan.FromSeconds(_options.PollIntervalSeconds)
                : TimeSpan.FromSeconds(_options.BackfillPollIntervalSeconds);
            await Task.Delay(delay, stoppingToken);
        }
    }

    /// <summary>Fast path: process the most recent unprocessed recordings first.</summary>
    private async Task<bool> ProcessRecentAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var recordings = scope.ServiceProvider.GetRequiredService<IRecordingsService>();

        var batch = await recordings.GetNeedingTranscriptionAsync(
            limit: _options.RecentBatchSize,
            minDurationSeconds: 1.0,
            lookbackHours: Math.Max(1, _options.RecentLookbackMinutes / 60),
            ct: ct);

        if (batch.Count == 0) return false;

        await ProcessBatchAsync(batch, ct);
        return true;
    }

    /// <summary>Backfill: slowly process older unprocessed recordings when there is spare capacity.</summary>
    private async Task ProcessBackfillAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var recordings = scope.ServiceProvider.GetRequiredService<IRecordingsService>();

        var batch = await recordings.GetNeedingTranscriptionAsync(
            limit: _options.BackfillBatchSize,
            minDurationSeconds: 1.0,
            lookbackHours: _options.BackfillLookbackHours,
            ct: ct);

        if (batch.Count == 0) return;

        await ProcessBatchAsync(batch, ct);
    }

    private async Task ProcessBatchAsync(IReadOnlyList<Core.Dtos.RecordingDto> batch, CancellationToken ct)
    {
        var tasks = batch.Select(async rec =>
        {
            await _gate.WaitAsync(ct);
            try
            {
                // Each task gets its own scope (and therefore its own DbContext) so that
                // concurrent processing never shares a context instance.
                using var scope = _scopeFactory.CreateScope();
                await ProcessOneAsync(scope, rec, ct);
            }
            finally
            {
                _gate.Release();
            }
        });
        await Task.WhenAll(tasks);
    }

    private async Task ProcessOneAsync(IServiceScope scope, Core.Dtos.RecordingDto rec, CancellationToken ct)
    {
        var recordings = scope.ServiceProvider.GetRequiredService<IRecordingsService>();
        var transcriptions = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();
        var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
        var ai = scope.ServiceProvider.GetRequiredService<IAiClient>();

        var stream = await recordings.GetFileAsync(rec.Id, ct);
        if (stream == null)
        {
            _logger.LogWarning("Recording {Id} file not found, marking processed.", rec.Id);
            await recordings.MarkProcessedAsync(rec.Id, ct);
            return;
        }

        using (stream)
        {
            // Skip if a final transcription already exists (avoids duplicate key errors)
            var existing = await db.Transcriptions
                .AnyAsync(t => t.RecordingId == rec.Id && t.IsFinal, ct);
            if (existing)
            {
                _logger.LogDebug("Recording {Id} already has a final transcription, skipping.", rec.Id);
                await recordings.MarkProcessedAsync(rec.Id, ct);
                return;
            }

            var result = await _asr.TranscribeAsync(stream, "wav", ct);
            var transcription = await transcriptions.CreateAsync(
                rec.Id, result.Service, result.Text, null, result.Confidence, null, isFinal: true, ct);

            // Embedding step (feature-gated). Only when there is text to embed.
            if (_features.Embeddings && !string.IsNullOrWhiteSpace(result.Text))
            {
                try
                {
                    if (ai.AreEmbeddingsEnabled)
                    {
                        var embedding = await ai.EmbedAsync(result.Text, ct);
                        if (embedding.Length > 0)
                        {
                            await EmbeddingStore.SetTranscriptionEmbeddingAsync(db, transcription.Id, embedding, ct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Embedding failure must not block transcription.
                    _logger.LogWarning(ex, "Failed to embed transcription {Id}.", transcription.Id);
                }
            }

            await recordings.MarkProcessedAsync(rec.Id, ct);
            _logger.LogInformation("Processed recording {Id} ({Chars} chars, embedded={Embedded}).",
                rec.Id, result.Text.Length, _features.Embeddings);

            await _notifier.NotifyNewTranscriptionAsync(rec.Id, transcription.Id, ct);
        }
    }
}
