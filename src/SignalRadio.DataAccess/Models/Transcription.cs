namespace SignalRadio.DataAccess;

public class Transcription
{
    public int Id { get; set; }
    public int RecordingId { get; set; }
    public Recording? Recording { get; set; }

    public string Service { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string FullText { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public string? AdditionalDataJson { get; set; }
    // stored in UTC
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsFinal { get; set; }
    
    // AI Summary fields
    public bool HasSummary { get; set; }
    public string? SummaryText { get; set; }
    public string? SummaryModel { get; set; }
    public double? SummaryConfidence { get; set; }
    public DateTimeOffset? SummaryGeneratedAt { get; set; }
    public long? SummaryProcessingTimeMs { get; set; }
    public string? LastSummaryError { get; set; }
    public int SummaryAttempts { get; set; }
}
