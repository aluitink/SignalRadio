namespace SignalRadio.Api.Features;

/// <summary>
/// Trigger criteria + tuning for the summaries agent. Bound from the "Summaries" config section.
///
/// Summarization is per-talkgroup over a rolling time window (not per-call). The agent is
/// triggered for a talkgroup only when enough new transcripts have accumulated since the
/// last summary, keeping AI cost proportional to actual radio activity.
/// </summary>
public class SummariesOptions
{
    public const string SectionName = "Summaries";

    /// <summary>Rolling window (minutes) used when a talkgroup has no prior summary.</summary>
    public int WindowMinutes { get; set; } = 30;

    /// <summary>Minimum number of new transcripts required before the agent runs for a talkgroup.</summary>
    public int MinTranscripts { get; set; } = 5;

    /// <summary>Cap on transcripts bundled into a single agent run (cost control).</summary>
    public int MaxTranscriptsPerRun { get; set; } = 50;

    /// <summary>How often the summaries service scans for talkgroups that are due.</summary>
    public int PollIntervalSeconds { get; set; } = 60;
}
