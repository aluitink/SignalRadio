namespace SignalRadio.Data.Entities;

public class Transcription
{
    public long Id { get; set; }
    public long RecordingId { get; set; }
    public Recording? Recording { get; set; }
    public string Service { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string FullText { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public string? AdditionalDataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsFinal { get; set; }

    // Search support (populated by database triggers / computed columns)
    // Note: This property is ignored by EF Core and managed via raw SQL for pgvector
    public byte[]? Embedding { get; set; }
}
