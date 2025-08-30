namespace SignalRadio.DataAccess;

public class Call
{
    public int Id { get; set; }

    public int TalkGroupId { get; set; }
    public TalkGroup? TalkGroup { get; set; }

    // stored in UTC (column name will include Utc)
    public DateTimeOffset RecordingTime { get; set; }
    public double FrequencyHz { get; set; }
    public int DurationSeconds { get; set; }
    // stored in UTC
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
}
