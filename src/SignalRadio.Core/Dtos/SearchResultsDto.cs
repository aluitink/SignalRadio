namespace SignalRadio.Core.Dtos;

public record SearchResultsDto(
    IReadOnlyList<TranscriptionSearchResultDto> Transcriptions,
    IReadOnlyList<TranscriptSummaryDto> Summaries,
    IReadOnlyList<NotableIncidentDto> Incidents,
    int TotalCount,
    int Page,
    int PageSize);

public record TranscriptionSearchResultDto(
    long TranscriptionId,
    long RecordingId,
    long CallId,
    int TalkGroupId,
    string TalkGroupNumber,
    string? TalkGroupName,
    DateTimeOffset RecordingTime,
    string FullText,
    double? Confidence,
    string Service,
    double? RelevanceScore);
