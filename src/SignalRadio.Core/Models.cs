using System.ComponentModel.DataAnnotations;

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
    [Required]
    public string TalkgroupId { get; set; } = string.Empty;

    [Required]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    public string SystemName { get; set; } = string.Empty;
}
