namespace SignalRadio.Core.Dtos;

public record TranscriptSummaryDto(
    long Id,
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TranscriptCount,
    double TotalDurationSeconds,
    string Summary,
    DateTime GeneratedAt,
    IReadOnlyList<TopicDto> Topics,
    IReadOnlyList<NotableIncidentDto> NotableIncidents);

public record CreateTranscriptSummaryRequest(
    int TalkGroupId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TranscriptCount,
    double TotalDurationSeconds,
    string Summary,
    IReadOnlyList<int> TopicIds,
    IReadOnlyList<long> NotableIncidentIds);
