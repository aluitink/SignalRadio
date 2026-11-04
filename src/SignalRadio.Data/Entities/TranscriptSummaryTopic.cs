namespace SignalRadio.Data.Entities;

public class TranscriptSummaryTopic
{
    public long TranscriptSummaryId { get; set; }
    public TranscriptSummary? TranscriptSummary { get; set; }
    public int TopicId { get; set; }
    public Topic? Topic { get; set; }
}
