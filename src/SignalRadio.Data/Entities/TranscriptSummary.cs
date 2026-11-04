namespace SignalRadio.Data.Entities;

public class TranscriptSummary
{
    public long Id { get; set; }
    public int TalkGroupId { get; set; }
    public TalkGroup? TalkGroup { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int TranscriptCount { get; set; }
    public double TotalDurationSeconds { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Note: This property is ignored by EF Core and managed via raw SQL for pgvector
    public byte[]? Embedding { get; set; }

    public ICollection<TranscriptSummaryTopic> TranscriptSummaryTopics { get; set; } = new List<TranscriptSummaryTopic>();
    public ICollection<TranscriptSummaryNotableIncident> TranscriptSummaryNotableIncidents { get; set; } = new List<TranscriptSummaryNotableIncident>();
}
