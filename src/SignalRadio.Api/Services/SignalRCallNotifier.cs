using Microsoft.AspNetCore.SignalR;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

public class SignalRCallNotifier(IHubContext<TalkGroupHub> hubContext) : ICallNotifier
{
    public Task NotifyNewCallAsync(int talkGroupId, long callId, CancellationToken ct = default) =>
        hubContext.Clients.Group($"talkgroup-{talkGroupId}").SendAsync("NewCall", callId, ct);

    /// <summary>
    /// Broadcasts a new call to all clients (live stream) and to the talk group's group
    /// (talk group pages). The full call payload lets clients prepend the card and
    /// enqueue it for auto-play without an extra API round-trip.
    /// </summary>
    public Task NotifyNewCallAsync(CallDto call, CancellationToken ct = default) =>
        Task.WhenAll(
            hubContext.Clients.All.SendAsync("NewCall", call, ct),
            hubContext.Clients.Group($"talkgroup-{call.TalkGroupId}").SendAsync("NewCall", call, ct));

    public Task NotifyNewTranscriptionAsync(long recordingId, long transcriptionId, CancellationToken ct = default) =>
        hubContext.Clients.All.SendAsync("NewTranscription", recordingId, transcriptionId, ct);

    public Task NotifyNewSummaryAsync(int talkGroupId, long summaryId, CancellationToken ct = default) =>
        hubContext.Clients.Group($"talkgroup-{talkGroupId}").SendAsync("NewSummary", summaryId, ct);
}
