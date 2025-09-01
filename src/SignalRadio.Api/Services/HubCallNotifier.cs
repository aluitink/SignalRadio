using Microsoft.AspNetCore.SignalR;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Services;

namespace SignalRadio.Api.Services;

public class HubCallNotifier : ICallNotifier
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HubCallNotifier> _logger;

    public HubCallNotifier(IServiceProvider services, ILogger<HubCallNotifier> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task NotifyCallUpdatedAsync(int callId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _services.CreateScope();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TalkGroupHub>>();
            var callsService = scope.ServiceProvider.GetRequiredService<SignalRadio.DataAccess.Services.ICallsService>();

            var updatedCall = await callsService.GetByIdAsync(callId);
            if (updatedCall == null)
            {
                return;
            }

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

            await hubContext.Clients.Group("all_calls_monitor").SendAsync("CallUpdated", callNotification, cancellationToken);
            await hubContext.Clients.Group($"talkgroup_{updatedCall.TalkGroupId}").SendAsync("CallUpdated", callNotification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify call update for call {CallId}", callId);
        }
    }
}
