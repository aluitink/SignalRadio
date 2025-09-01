namespace SignalRadio.DataAccess;

public class TalkGroup
{
    public int Id { get; set; }
    public int Number { get; set; }
    // Human readable name / description
    public string? Name { get; set; }

    // Short alpha tag (e.g. "DaneCom")
    public string? AlphaTag { get; set; }

    // Secondary tag / short identifier
    public string? Tag { get; set; }

    // Longer description of the talkgroup
    public string? Description { get; set; }

    // Category for grouping talkgroups (optional)
    public string? Category { get; set; }

    // Priority is used in ordering for transcription prioritization.
    public int? Priority { get; set; }

    public ICollection<Call> Calls { get; set; } = new List<Call>();
}
