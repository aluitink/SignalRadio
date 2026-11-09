using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ICallNotifier
{
    Task NotifyNewCallAsync(int talkGroupId, long callId, CancellationToken ct = default);
    Task NotifyNewCallAsync(CallDto call, CancellationToken ct = default);
    Task NotifyNewTranscriptionAsync(long recordingId, long transcriptionId, CancellationToken ct = default);
    Task NotifyNewSummaryAsync(int talkGroupId, long summaryId, CancellationToken ct = default);
    Task NotifyInvestigationStartedAsync(long investigationId, long callId, CancellationToken ct = default);
    Task NotifyInvestigationCompletedAsync(long investigationId, long callId, string status, CancellationToken ct = default);

    /// <summary>
    /// Streams a single investigation chat event (text delta, tool call, tool result, run
    /// finished/failed) to clients subscribed to the investigation's SignalR group.
    /// The payload is a JSON-serializable <c>InvestigationChatEvent</c>.
    /// </summary>
    Task NotifyInvestigationChatEventAsync(long investigationId, object chatEvent, CancellationToken ct = default);
}
