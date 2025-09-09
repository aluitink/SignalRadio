namespace SignalRadio.DataAccess;

/// <summary>
/// Represents a notable incident or event that may involve multiple calls
/// </summary>
public class NotableIncident
{
    public int Id { get; set; }

    /// <summary>
    /// Description of the notable incident/event
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional importance/severity score
    /// </summary>
    public double? ImportanceScore { get; set; }

    /// <summary>
    /// When this incident was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property for calls associated with this incident
    /// </summary>
    public ICollection<NotableIncidentCall> NotableIncidentCalls { get; set; } = new List<NotableIncidentCall>();

    /// <summary>
    /// Navigation property for summaries that reference this incident
    /// </summary>
    public ICollection<TranscriptSummaryNotableIncident> TranscriptSummaryNotableIncidents { get; set; } = new List<TranscriptSummaryNotableIncident>();
}
