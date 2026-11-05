using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ITranscriptSummariesService
{
    Task<IReadOnlyList<TranscriptSummaryDto>> GetByTalkGroupAsync(int talkGroupId, DateTimeOffset? startTime, DateTimeOffset? endTime, CancellationToken ct = default);
    Task<IReadOnlyList<TranscriptSummaryDto>> GetRecentAsync(int count = 10, CancellationToken ct = default);
    Task<TranscriptSummaryDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// The talkgroup's most recent prior summary (by EndTime), or null if none.
    /// Used by the summaries agent to note what is new versus ongoing.
    /// </summary>
    Task<TranscriptSummaryDto?> GetLatestForTalkGroupAsync(int talkGroupId, CancellationToken ct = default);

    Task<TranscriptSummaryDto> CreateAsync(CreateTranscriptSummaryRequest request, CancellationToken ct = default);
}
