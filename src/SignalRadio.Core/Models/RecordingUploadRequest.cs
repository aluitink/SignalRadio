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
}
