namespace SignalRadio.DataAccess;

/// <summary>
/// Linking table between TranscriptSummary and Topic (many-to-many)
/// </summary>
public class TranscriptSummaryTopic
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the transcript summary
    /// </summary>
    public int TranscriptSummaryId { get; set; }
    public TranscriptSummary? TranscriptSummary { get; set; }

    /// <summary>
    /// Reference to the topic
    /// </summary>
    public int TopicId { get; set; }
    public Topic? Topic { get; set; }

    /// <summary>
    /// Optional relevance score for this topic in this specific summary
    /// </summary>
    public double? Relevance { get; set; }

    /// <summary>
    /// When this association was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
