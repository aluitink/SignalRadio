namespace SignalRadio.Core.Dtos;

public record InvestigationDto(
    long Id,
    long CallId,
    string CallTalkGroupNumber,
    string? CallTalkGroupName,
    DateTimeOffset CallRecordingTime,
    int WindowMinutes,
    string Prompt,
    long? ParentInvestigationId,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ResultJson,
    string? Error,
    bool IsViewed);

public record CreateInvestigationRequest(
    long CallId,
    string Prompt,
    int WindowMinutes = 30,
    int? TalkGroupId = null,
    long? ParentInvestigationId = null);
