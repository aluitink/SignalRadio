namespace SignalRadio.Data.Entities;

public class NotableIncident
{
    public long Id { get; set; }
    public int? TalkGroupId { get; set; }
    public TalkGroup? TalkGroup { get; set; }
    public string Description { get; set; } = string.Empty;
    public double? ImportanceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<NotableIncidentCall> NotableIncidentCalls { get; set; } = new List<NotableIncidentCall>();
    public ICollection<TranscriptSummaryNotableIncident> TranscriptSummaryNotableIncidents { get; set; } = new List<TranscriptSummaryNotableIncident>();
}
