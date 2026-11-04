namespace SignalRadio.Core.Dtos;

/// <summary>
/// Metadata posted by trunk-recorder's upload callback (multipart "metadata" field).
/// Field names must match what scripts/upload_callback.sh sends.
/// </summary>
public class RecordingUploadRequest
{
    public required string TalkgroupId { get; init; }
    public required string Frequency { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string SystemName { get; init; }
    public double? Duration { get; init; }
    public long? StopTime { get; init; }
}
