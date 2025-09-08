namespace SignalRadio.Core.Models;

public class TranscriptSummaryRequest
{
    /// <summary>
    /// TalkGroup ID to summarize transcripts for
    /// </summary>
    public int TalkGroupId { get; set; }

    /// <summary>
    /// Start time for the summary window (UTC)
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// End time for the summary window (UTC)
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Force refresh of cached summary
    /// </summary>
    public bool ForceRefresh { get; set; } = false;
}

public class TranscriptSummaryResponse
{
    /// <summary>
    /// TalkGroup ID
    /// </summary>
    public int TalkGroupId { get; set; }

    /// <summary>
    /// TalkGroup information
    /// </summary>
    public string TalkGroupName { get; set; } = string.Empty;

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
    /// Key topics identified from the transcripts
    /// </summary>
    public List<string> KeyTopics { get; set; } = new();

    /// <summary>
    /// Notable incidents or events mentioned
    /// </summary>
    public List<string> NotableIncidents { get; set; } = new();

    /// <summary>
    /// Notable incidents with call ID references for linking
    /// </summary>
    public List<NotableIncident> NotableIncidentsWithCallIds { get; set; } = new();

    /// <summary>
    /// When this summary was generated (UTC)
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// Whether this summary was retrieved from cache
    /// </summary>
    public bool FromCache { get; set; } = false;
}

/// <summary>
/// Represents a notable incident with references to related calls
/// </summary>
public class NotableIncident
{
    /// <summary>
    /// Description of the incident
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Call IDs related to this incident
    /// </summary>
    public List<int> CallIds { get; set; } = new();
}
