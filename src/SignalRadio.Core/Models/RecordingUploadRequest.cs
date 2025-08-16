using System.ComponentModel.DataAnnotations;

namespace SignalRadio.Core.Models;

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

    /// <summary>
    /// Call duration in seconds (can be fractional)
    /// </summary>
    public double? Duration { get; set; }

    /// <summary>
    /// Call stop time (Unix timestamp)
    /// </summary>
    public long? StopTime { get; set; }
}
