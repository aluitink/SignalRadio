using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ICallNotifier
{
    Task NotifyNewCallAsync(int talkGroupId, long callId, CancellationToken ct = default);
    Task NotifyNewCallAsync(CallDto call, CancellationToken ct = default);
    Task NotifyNewTranscriptionAsync(long recordingId, long transcriptionId, CancellationToken ct = default);
    Task NotifyNewSummaryAsync(int talkGroupId, long summaryId, CancellationToken ct = default);
}
