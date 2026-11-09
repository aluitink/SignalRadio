namespace SignalRadio.Data.Entities;

/// <summary>
/// Join row linking a <see cref="TranscriptSummary"/> to the <see cref="Call"/>
/// whose final transcript was folded into the summary. Lets the UI list the
/// calls that made up a summary so they can be reviewed and played back.
/// </summary>
public class TranscriptSummaryCall
{
    public long TranscriptSummaryId { get; set; }
    public TranscriptSummary? TranscriptSummary { get; set; }
    public long CallId { get; set; }
    public Call? Call { get; set; }
}
