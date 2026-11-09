namespace SignalRadio.Core.Dtos;

public record TranscriptSummaryDto(
    long Id,
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
    string? TalkGroupDescription,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TranscriptCount,
    double TotalDurationSeconds,
    string Summary,
    int? Severity,
    DateTime GeneratedAt,
    IReadOnlyList<TopicDto> Topics,
    IReadOnlyList<NotableIncidentDto> NotableIncidents,
    IReadOnlyList<long> CallIds);

/// <summary>
/// A call that was folded into a transcript summary, with just enough
/// metadata to render a call card and play the audio.
/// </summary>
public record SummaryCallDto(
    long CallId,
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
    string? TalkGroupDescription,
    DateTimeOffset RecordingTime,
    double FrequencyHz,
    double DurationSeconds,
    long? LatestRecordingId);

/// <summary>
/// Cursor-paged list of summaries (newest first). `NextCursor` is the
/// (EndTime, Id) of the oldest summary on the page, used to request the
/// next page.
/// </summary>
public record PagedSummarySearchResult(
    IReadOnlyList<TranscriptSummaryDto> Summaries,
    DateTimeOffset? NextCursorTime,
    long? NextCursorId,
    bool HasMore);

public record CreateTranscriptSummaryRequest(
    int TalkGroupId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TranscriptCount,
    double TotalDurationSeconds,
    string Summary,
    int? Severity,
    IReadOnlyList<int> TopicIds,
    IReadOnlyList<long> NotableIncidentIds,
    IReadOnlyList<long> CallIds);
