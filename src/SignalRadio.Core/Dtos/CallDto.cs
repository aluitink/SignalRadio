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
    int RecordingCount);

public record CreateCallRequest(
    int TalkGroupId,
    DateTimeOffset RecordingTime,
    double FrequencyHz,
    double DurationSeconds);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
