namespace SignalRadio.Data.Entities;

public class TranscriptSummaryNotableIncident
{
    public long TranscriptSummaryId { get; set; }
    public TranscriptSummary? TranscriptSummary { get; set; }
    public long NotableIncidentId { get; set; }
    public NotableIncident? NotableIncident { get; set; }
}
