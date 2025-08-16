using System.ComponentModel.DataAnnotations;

namespace SignalRadio.Core.Models;

// Database entities
public class Call
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string TalkgroupId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string SystemName { get; set; } = string.Empty;
    
    [Required]
    public DateTime RecordingTime { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Frequency { get; set; } = string.Empty;
    
    public TimeSpan? Duration { get; set; }
    
    // Navigation property for recordings
    public virtual ICollection<Recording> Recordings { get; set; } = new List<Recording>();
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
