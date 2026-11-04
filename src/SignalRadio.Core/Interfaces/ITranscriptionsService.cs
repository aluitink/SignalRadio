using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ITranscriptionsService
{
    Task<TranscriptionDto> CreateAsync(long recordingId, string service, string fullText, string? language, double? confidence, string? additionalDataJson, bool isFinal, CancellationToken ct = default);
    Task<IReadOnlyList<TranscriptionDto>> GetByRecordingIdAsync(long recordingId, CancellationToken ct = default);
    Task<TranscriptionDto?> GetFinalByRecordingIdAsync(long recordingId, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}
