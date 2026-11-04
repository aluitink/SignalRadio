using Microsoft.AspNetCore.SignalR;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

public class SignalRCallNotifier(IHubContext<TalkGroupHub> hubContext) : ICallNotifier
{
    public Task NotifyNewCallAsync(int talkGroupId, long callId, CancellationToken ct = default) =>
        hubContext.Clients.Group($"talkgroup-{talkGroupId}").SendAsync("NewCall", callId, ct);

    public Task NotifyNewTranscriptionAsync(long recordingId, long transcriptionId, CancellationToken ct = default) =>
        hubContext.Clients.All.SendAsync("NewTranscription", recordingId, transcriptionId, ct);

    public Task NotifyNewSummaryAsync(int talkGroupId, long summaryId, CancellationToken ct = default) =>
        hubContext.Clients.Group($"talkgroup-{talkGroupId}").SendAsync("NewSummary", summaryId, ct);
}
