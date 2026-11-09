using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;
using SignalRadio.Data.Entities;

namespace SignalRadio.Api.Services;

/// <summary>
/// Background worker that processes queued investigations. Polls the database for
/// pending investigations and runs the investigate agent as a streaming chat turn via
/// <see cref="InvestigationChatService"/> (which persists the resumable MAF session and
/// relays events to the UI over SignalR).
///
/// Uses a SemaphoreSlim to limit concurrent agent runs (AI calls are expensive).
/// </summary>
public class InvestigationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICallNotifier _notifier;
    private readonly InvestigationChatService _chat;
    private readonly ILogger<InvestigationWorker> _logger;
    private readonly SemaphoreSlim _gate;
    private const int MaxConcurrent = 2;
    private const int PollIntervalMs = 2000;

    public InvestigationWorker(
        IServiceScopeFactory scopeFactory,
        ICallNotifier notifier,
        InvestigationChatService chat,
        ILogger<InvestigationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _chat = chat;
        _logger = logger;
        _gate = new SemaphoreSlim(MaxConcurrent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Investigation worker started (maxConcurrent={Max}).", MaxConcurrent);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Investigation worker error.");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        // Find pending investigations (oldest first).
        List<Investigation> pending;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
            pending = await db.Investigations
                .Where(i => i.Status == InvestigationStatus.Pending)
                .OrderBy(i => i.CreatedAt)
                .Take(MaxConcurrent)
                .ToListAsync(ct);
        }

        if (pending.Count == 0) return;

        foreach (var inv in pending)
        {
            await _gate.WaitAsync(ct);
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessOneAsync(inv.Id, ct);
                }
                finally
                {
                    _gate.Release();
                }
            }, ct);
        }
    }

    private async Task ProcessOneAsync(long investigationId, CancellationToken ct)
    {
        _logger.LogInformation("Processing investigation {Id}.", investigationId);

        // Mark as running.
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
            var inv = await db.Investigations.FindAsync(new object[] { investigationId }, ct);
            if (inv is null || inv.Status != InvestigationStatus.Pending) return;

            inv.Status = InvestigationStatus.Running;
            inv.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        await _notifier.NotifyInvestigationStartedAsync(investigationId, 0, ct);

        // Run the agent as a streaming chat turn. The chat service persists the resumable
        // MAF session, stores the structured report, and relays events to the UI.
        await _chat.RunTurnAsync(investigationId, "Investigate this call and determine what event it is part of.", ct);
    }
}
