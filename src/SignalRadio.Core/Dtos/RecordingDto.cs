namespace SignalRadio.Core.Dtos;

public record RecordingDto(
    long Id,
    long CallId,
    int StorageLocationId,
    string FileName,
    long SizeBytes,
    DateTimeOffset ReceivedAt,
    bool IsProcessed,
    string? DownloadUrl);
