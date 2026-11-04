namespace SignalRadio.Data.Entities;

public class Call
{
    public long Id { get; set; }
    public int TalkGroupId { get; set; }
    public TalkGroup? TalkGroup { get; set; }
    public DateTimeOffset RecordingTime { get; set; }
    public double FrequencyHz { get; set; }
    public double DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
}
