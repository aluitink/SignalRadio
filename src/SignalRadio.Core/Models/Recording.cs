using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignalRadio.Core.Models;

public class Recording
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CallId { get; set; }
    
    [ForeignKey(nameof(CallId))]
    public virtual Call Call { get; set; } = null!;
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Format { get; set; } = string.Empty; // WAV, M4A, MP3, OGG
    
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [MaxLength(1000)]
    public string? BlobUri { get; set; }
    
    [MaxLength(500)]
    public string? BlobName { get; set; }
    
    // Storage metadata
    public bool IsUploaded { get; set; }
    public DateTime? UploadedAt { get; set; }
    
    // Enhanced metadata
    public TimeSpan? Duration { get; set; } // Audio duration if available
    public int? SampleRate { get; set; } // Audio sample rate
    public int? Bitrate { get; set; } // Audio bitrate
    public byte? Channels { get; set; } // Audio channels (1=mono, 2=stereo)
    
    // Computed properties for efficiency
    [MaxLength(10)]
    public string? Quality { get; set; } // LOW, MEDIUM, HIGH based on bitrate/format
    
    [MaxLength(32)]
    public string? FileHash { get; set; } // MD5 hash for deduplication
    
    // Upload tracking
    public int UploadAttempts { get; set; } = 0;
    public string? LastUploadError { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Computed properties for queries
    [NotMapped]
    public double FileSizeMB => Math.Round(FileSize / 1024.0 / 1024.0, 2);
    
    [NotMapped]
    public string FormattedDuration => Duration?.ToString(@"mm\:ss") ?? "Unknown";
    
    [NotMapped]
    public bool IsHighQuality => Quality == "HIGH" || (Bitrate >= 128 && Format == "M4A") || (Bitrate >= 256 && Format == "WAV");
}
