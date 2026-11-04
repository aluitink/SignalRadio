namespace SignalRadio.Core.Dtos;

public record TranscriptionDto(
    long Id,
    long RecordingId,
    string Service,
    string? Language,
    string FullText,
    double? Confidence,
    string? AdditionalDataJson,
    DateTime CreatedAt,
    bool IsFinal);
