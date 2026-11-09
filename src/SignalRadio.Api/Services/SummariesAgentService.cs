using Microsoft.Extensions.AI;
using SignalRadio.Api.Agents;
using SignalRadio.Api.Features;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;

namespace SignalRadio.Api.Services;

/// <summary>
/// Background service that produces per-talkgroup, over-time summaries using the summaries agent.
///
/// For each talkgroup it finds the most recent summary, collects the final transcripts that have
/// landed since then, and — once enough have accumulated — runs the agent to produce a new
/// rolling summary. The agent is given tools to fetch the previous summary and talkgroup info,
/// so it can build on prior context rather than summarizing in a vacuum.
///
/// Gated by <see cref="FeatureFlags.Summaries"/>.
/// </summary>
public class SummariesAgentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatClient _chatClient;
    private readonly AgentRegistry _registry;
    private readonly FeatureFlags _features;
    private readonly SummariesOptions _options;
    private readonly ILogger<SummariesAgentService> _logger;

    public SummariesAgentService(
        IServiceScopeFactory scopeFactory,
        IChatClient chatClient,
        AgentRegistry registry,
        FeatureFlags features,
        SummariesOptions options,
        ILogger<SummariesAgentService> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _registry = registry;
        _features = features;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_features.Summaries)
        {
            _logger.LogInformation("Summaries agent disabled (Features:Summaries=false). Idling.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _logger.LogInformation("Summaries agent started (poll={Poll}s, minTranscripts={Min}).",
            _options.PollIntervalSeconds, _options.MinTranscripts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in summaries agent run.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var talkGroups = sp.GetRequiredService<ITalkGroupsService>();
        var transcriptions = sp.GetRequiredService<ITranscriptionsService>();
        var summaries = sp.GetRequiredService<ITranscriptSummariesService>();
        var ai = sp.GetRequiredService<IAiClient>();
        var notifier = sp.GetRequiredService<ICallNotifier>();
        var db = sp.GetRequiredService<SignalRadioDbContext>();

        var groups = await talkGroups.GetAllAsync(ct);
        var agent = _registry.Build("summaries", _chatClient, () => SummariesAgent.CreateTools(sp));

        foreach (var tg in groups)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var last = await summaries.GetLatestForTalkGroupAsync(tg.Id, ct);
                var since = last?.EndTime.UtcDateTime ?? DateTime.UtcNow.AddMinutes(-_options.WindowMinutes);

                var transcripts = await transcriptions.GetFinalByTalkGroupSinceAsync(tg.Id, since, _options.MaxTranscriptsPerRun, ct);
                if (transcripts.Count < _options.MinTranscripts) continue;

                var bundle = BuildTranscriptBundle(transcripts);
                var prompt = $"Summarize the following {transcripts.Count} radio calls on talkgroup {tg.Id} ({tg.Name}) that occurred since {since:yyyy-MM-dd HH:mm} UTC:\n\n{bundle}";

                var response = await agent.RunAsync<SummaryReport>(prompt, cancellationToken: ct);
                var report = response?.Result;
                if (report == null || string.IsNullOrWhiteSpace(report.Summary)) continue;

                var summaryText = report.Summary.Trim();
                var totalDuration = transcripts.Sum(t => t.DurationSeconds);

                // Record the calls that made up this summary so the UI can list
                // and play them back. De-duplicated, oldest first.
                var callIds = transcripts
                    .Select(t => t.CallId)
                    .Where(id => id > 0)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                var created = await summaries.CreateAsync(new CreateTranscriptSummaryRequest(
                    tg.Id,
                    new DateTimeOffset(since, TimeSpan.Zero),
                    new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                    transcripts.Count,
                    totalDuration,
                    summaryText,
                    report.Severity,
                    Array.Empty<int>(),
                    Array.Empty<long>(),
                    callIds), ct);

                if (_features.Embeddings && ai.AreEmbeddingsEnabled)
                {
                    try
                    {
                        var embedding = await ai.EmbedAsync(summaryText, ct);
                        if (embedding.Length > 0)
                        {
                            await EmbeddingStore.SetSummaryEmbeddingAsync(db, created.Id, embedding, ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to embed summary {Id}.", created.Id);
                    }
                }

                await notifier.NotifyNewSummaryAsync(tg.Id, created.Id, ct);
                _logger.LogInformation("Created summary {Id} for talkgroup {Tg} ({Count} transcripts).",
                    created.Id, tg.Id, transcripts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to summarize talkgroup {Tg}.", tg.Id);
            }
        }
    }

    private static string BuildTranscriptBundle(IReadOnlyList<TranscriptionDto> transcripts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var t in transcripts)
        {
            sb.AppendLine($"[{t.CreatedAt:yyyy-MM-dd HH:mm:ss}] {t.FullText}");
        }
        return sb.ToString();
    }
}
