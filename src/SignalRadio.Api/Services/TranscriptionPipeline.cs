using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;

namespace SignalRadio.Api.Services;

public class TranscriptionPipeline(
    IServiceScopeFactory scopeFactory,
    IAsrService asr,
    ICallNotifier notifier,
    ILogger<TranscriptionPipeline> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Transcription pipeline started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in transcription pipeline.");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var recordings = scope.ServiceProvider.GetRequiredService<IRecordingsService>();
        var transcriptions = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();
        var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();

        var batch = await recordings.GetNeedingTranscriptionAsync(limit: 10, minDurationSeconds: 1.0, lookbackHours: 24, ct);
        if (batch.Count == 0) return;

        foreach (var rec in batch)
        {
            try
            {
                var stream = await recordings.GetFileAsync(rec.Id, ct);
                if (stream == null)
                {
                    logger.LogWarning("Recording {Id} file not found, skipping.", rec.Id);
                    await recordings.MarkProcessedAsync(rec.Id, ct);
                    continue;
                }

                var result = await asr.TranscribeAsync(stream, "wav", ct);
                await transcriptions.CreateAsync(
                    rec.Id,
                    result.Service,
                    result.Text,
                    null,
                    result.Confidence,
                    null,
                    true,
                    ct);

                await recordings.MarkProcessedAsync(rec.Id, ct);
                logger.LogInformation("Transcribed recording {Id} ({Chars} chars).", rec.Id, result.Text.Length);

                // Notify via SignalR
                await notifier.NotifyNewTranscriptionAsync(rec.Id, 0, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to transcribe recording {Id}.", rec.Id);
            }
        }
    }
}
