namespace SignalRadio.Data.Entities;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TranscriptSummaryTopic> TranscriptSummaryTopics { get; set; } = new List<TranscriptSummaryTopic>();
}
