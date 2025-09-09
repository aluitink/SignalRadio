namespace SignalRadio.DataAccess;

/// <summary>
/// Represents a reusable topic that can be associated with multiple summaries
/// </summary>
public class Topic
{
    public int Id { get; set; }

    /// <summary>
    /// The topic/keyword text
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional category for grouping topics
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// When this topic was first created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property for summaries that reference this topic
    /// </summary>
    public ICollection<TranscriptSummaryTopic> TranscriptSummaryTopics { get; set; } = new List<TranscriptSummaryTopic>();
}
