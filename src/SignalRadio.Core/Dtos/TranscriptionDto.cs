namespace SignalRadio.Core.Dtos;

public record TranscriptionDto(
    long Id,
    long RecordingId,
    long CallId,
    string Service,
    string? Language,
    string FullText,
    double? Confidence,
    string? AdditionalDataJson,
    DateTime CreatedAt,
    bool IsFinal,
    double DurationSeconds);
