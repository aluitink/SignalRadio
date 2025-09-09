namespace SignalRadio.DataAccess;

/// <summary>
/// Linking table between TranscriptSummary and NotableIncident (many-to-many)
/// </summary>
public class TranscriptSummaryNotableIncident
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the transcript summary
    /// </summary>
    public int TranscriptSummaryId { get; set; }
    public TranscriptSummary? TranscriptSummary { get; set; }

    /// <summary>
    /// Reference to the notable incident
    /// </summary>
    public int NotableIncidentId { get; set; }
    public NotableIncident? NotableIncident { get; set; }

    /// <summary>
    /// When this association was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
