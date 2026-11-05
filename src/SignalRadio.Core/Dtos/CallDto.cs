namespace SignalRadio.Core.Dtos;

public record CallDto(
    long Id,
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
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

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
