using Microsoft.AspNetCore.SignalR;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

public class SignalRCallNotifier(IHubContext<TalkGroupHub> hubContext) : ICallNotifier
{
    /// <summary>
    /// Broadcasts a new call to every connected client. The backend is a dumb pipe —
    /// it forwards all calls and lets each client's stream decide what to render
    /// based on its own filters. The full payload lets clients prepend the card and
    /// enqueue it for auto-play without an extra API round-trip.
    /// </summary>
    public Task NotifyNewCallAsync(int talkGroupId, long callId, CancellationToken ct = default) =>
        hubContext.Clients.All.SendAsync("NewCall", callId, ct);

    public Task NotifyNewCallAsync(CallDto call, CancellationToken ct = default) =>
        hubContext.Clients.All.SendAsync("NewCall", call, ct);

    public Task NotifyNewTranscriptionAsync(long recordingId, long transcriptionId, CancellationToken ct = default) =>
        hubContext.Clients.All.SendAsync("NewTranscription", recordingId, transcriptionId, ct);

    public Task NotifyNewSummaryAsync(int talkGroupId, long summaryId, CancellationToken ct = default) =>
        hubContext.Clients.Group($"talkgroup-{talkGroupId}").SendAsync("NewSummary", summaryId, ct);
}
