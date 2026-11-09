namespace SignalRadio.Core.Dtos;

public record CallDto(
    long Id,
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
    string? TalkGroupDescription,
    DateTimeOffset RecordingTime,
    double FrequencyHz,
    double DurationSeconds,
    DateTime CreatedAt,
    int RecordingCount,
    long? LatestRecordingId = null);

public record CreateCallRequest(
    int TalkGroupId,
    DateTimeOffset RecordingTime,
    double FrequencyHz,
    double DurationSeconds);

/// <summary>One bucket of a call-volume histogram: call count and total talk time in a time window.</summary>
public record CallVolumeDto(
    DateTimeOffset BucketStart,
    int CallCount,
    double TotalDurationSeconds);

/// <summary>Call volume for a single talk group within one histogram bucket.</summary>
public record TalkGroupVolumeDto(
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
    int CallCount,
    double TotalDurationSeconds);

/// <summary>One histogram bucket broken down by talk group (for stacked charts).</summary>
public record CallVolumeByTalkGroupDto(
    DateTimeOffset BucketStart,
    IReadOnlyList<TalkGroupVolumeDto> TalkGroups);

/// <summary>
/// Cursor-based page of results for "scroll back in time" feeds. The cursor is a
/// (RecordingTime, Id) pair that is stable even when new rows are inserted at the
/// top of the feed, unlike offset paging which can shift or duplicate rows.
/// </summary>
public record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    DateTimeOffset? NextCursorTime,
    long? NextCursorId,
    bool HasMore);
