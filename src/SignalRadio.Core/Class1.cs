namespace SignalRadio.Core.Models;

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
}

public class RecordingUploadRequest
{
    public string TalkgroupId { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SystemName { get; set; } = string.Empty;
}
