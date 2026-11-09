using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;

namespace SignalRadio.Simulator;

public class SimulatorService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<SimulatorService> logger) : BackgroundService
{
    private readonly Random _rng = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(config.GetValue<int>("Simulator:IntervalSeconds", 30));
        logger.LogInformation("Simulator started. Interval: {Interval}", interval);

        // Wait for DB to be ready
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SimulateCallAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Simulator error.");
            }
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task SimulateCallAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
        var talkGroups = scope.ServiceProvider.GetRequiredService<ITalkGroupsService>();
        var calls = scope.ServiceProvider.GetRequiredService<ICallsService>();
        var recordings = scope.ServiceProvider.GetRequiredService<IRecordingsService>();
        var notifier = scope.ServiceProvider.GetRequiredService<ICallNotifier>();

        // Pick a random talk group
        var groups = await talkGroups.GetAllAsync(ct);
        if (groups.Count == 0)
        {
            logger.LogWarning("No talk groups found. Create some first.");
            return;
        }
        var group = groups[_rng.Next(groups.Count)];

        // Create a call
        var duration = _rng.Next(5, 120);
        var call = await calls.CreateAsync(new CreateCallRequest(
            group.Id,
            DateTimeOffset.UtcNow,
            146.520,
            duration), ct);

        // Create a recording with a small silent WAV
        var wavData = CreateSilentWav(duration);
        var fileName = $"sim_{call.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.wav";
        using var stream = new MemoryStream(wavData);
        await recordings.UploadAsync(call.Id, fileName, stream, wavData.Length, ct);

        // Notify
        await notifier.NotifyNewCallAsync(group.Id, call.Id, ct);
        logger.LogInformation("Simulated call {CallId} on talk group {Group} ({Duration}s).", call.Id, group.Number, duration);

        // Occasionally generate a mock AI summary for this talk group so the
        // web UI's summary ticker has data to show while the simulator runs.
        if (_rng.NextDouble() < 0.35)
        {
            await TryCreateMockSummaryAsync(scope, group, ct);
        }
    }

    private async Task TryCreateMockSummaryAsync(IServiceScope scope, TalkGroupDto group, CancellationToken ct)
    {
        var summaries = scope.ServiceProvider.GetRequiredService<ITranscriptSummariesService>();
        var calls = scope.ServiceProvider.GetRequiredService<ICallsService>();
        var notifier = scope.ServiceProvider.GetRequiredService<ICallNotifier>();

        // Gather a handful of recent calls on this talk group to summarise.
        var recent = await calls.GetCursorAsync(pageSize: 5, talkGroupId: group.Id, ct: ct);
        if (recent.Items.Count == 0) return;

        var start = recent.Items[^1].RecordingTime;
        var end = recent.Items[0].RecordingTime;
        var totalDuration = recent.Items.Sum(c => c.DurationSeconds);

        var summaryText = PickMockSummary(group, recent.Items.Count);
        var severity = Random.Shared.Next(0, 11);
        var created = await summaries.CreateAsync(new CreateTranscriptSummaryRequest(
            group.Id,
            start,
            end,
            recent.Items.Count,
            totalDuration,
            summaryText,
            severity,
            Array.Empty<int>(),
            Array.Empty<long>(),
            recent.Items.Select(c => c.Id).ToList()), ct);

        await notifier.NotifyNewSummaryAsync(group.Id, created.Id, ct);
        logger.LogInformation("Simulated summary {SummaryId} for talk group {Group}.", created.Id, group.Number);
    }

    private string PickMockSummary(TalkGroupDto group, int callCount)
    {
        var templates = new[]
        {
            $"Routine traffic on {group.Name ?? group.Number}: {callCount} calls logged, no emergencies reported.",
            $"Unit check-in and status updates on {group.Name ?? group.Number}. Traffic nominal.",
            $"Dispatch coordination on {group.Name ?? group.Number}; {callCount} calls handled, no incidents.",
            $"Field units reporting on {group.Name ?? group.Number}. Weather advisory discussed, operations continuing.",
            $"Maintenance and logistics chatter on {group.Name ?? group.Number}. No actionable incidents.",
        };
        return templates[_rng.Next(templates.Length)];
    }

    private static byte[] CreateSilentWav(int durationSeconds)
    {
        const int sampleRate = 8000;
        const int bitsPerSample = 16;
        const int channels = 1;
        int numSamples = sampleRate * durationSeconds;
        int dataSize = numSamples * (bitsPerSample / 8);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * (bitsPerSample / 8));
        bw.Write((short)(channels * (bitsPerSample / 8)));
        bw.Write((short)bitsPerSample);

        // data chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);
        bw.Write(new byte[dataSize]);

        return ms.ToArray();
    }
}
