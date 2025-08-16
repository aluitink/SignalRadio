namespace SignalRadio.Core.Models;

// Existing DTOs
public class RecordingMetadata
{
    public string TalkgroupId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public DateTime RecordingTime { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFormat { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public string? BlobUri { get; set; }
    public string? BlobName { get; set; }
}
