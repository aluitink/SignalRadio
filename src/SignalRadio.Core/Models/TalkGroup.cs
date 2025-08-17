using System.ComponentModel.DataAnnotations;

namespace SignalRadio.Core.Models;

public class TalkGroup
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Decimal { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Hex { get; set; }
    
    [MaxLength(10)]
    public string? Mode { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string AlphaTag { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string? Tag { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    public int? Priority { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
