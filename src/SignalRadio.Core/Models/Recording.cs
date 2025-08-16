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
    [MaxLength(50)]
    public string Format { get; set; } = string.Empty; // WAV, M4A, etc.
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [MaxLength(500)]
    public string? BlobUri { get; set; }
    
    [MaxLength(255)]
    public string? BlobName { get; set; }
    
    // Storage metadata
    public bool IsUploaded { get; set; }
    public DateTime? UploadedAt { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
