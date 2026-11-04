using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ITranscriptSummariesService
{
    Task<IReadOnlyList<TranscriptSummaryDto>> GetByTalkGroupAsync(int talkGroupId, DateTimeOffset? startTime, DateTimeOffset? endTime, CancellationToken ct = default);
    Task<TranscriptSummaryDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<TranscriptSummaryDto> CreateAsync(CreateTranscriptSummaryRequest request, CancellationToken ct = default);
}
