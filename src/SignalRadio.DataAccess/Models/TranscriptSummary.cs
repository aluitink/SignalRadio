namespace SignalRadio.DataAccess;

/// <summary>
/// Represents a cached AI-generated summary of transcripts for a TalkGroup over a time period
/// </summary>
public class TranscriptSummary
{
    public int Id { get; set; }

    /// <summary>
    /// TalkGroup this summary is for
    /// </summary>
    public int TalkGroupId { get; set; }
    public TalkGroup? TalkGroup { get; set; }

    /// <summary>
    /// Start time for the summary window (UTC)
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// End time for the summary window (UTC)
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Number of transcripts included in the summary
    /// </summary>
    public int TranscriptCount { get; set; }

    /// <summary>
    /// Total duration of calls in seconds
    /// </summary>
    public double TotalDurationSeconds { get; set; }

    /// <summary>
    /// AI-generated summary of the transcripts
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// When this summary was generated (UTC)
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// When this summary was created in the database (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation properties for related topics and notable incidents
    /// </summary>
    public ICollection<TranscriptSummaryTopic> TranscriptSummaryTopics { get; set; } = new List<TranscriptSummaryTopic>();
    public ICollection<TranscriptSummaryNotableIncident> TranscriptSummaryNotableIncidents { get; set; } = new List<TranscriptSummaryNotableIncident>();
}
