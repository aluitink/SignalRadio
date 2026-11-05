namespace SignalRadio.Api.Features;

/// <summary>
/// Tuning for the inbound call-processing pipeline (ASR + optional embedding).
/// Bound from the "Pipeline" config section.
///
/// The pipeline processes the most recent unprocessed recordings first (fast path),
/// then slowly backfills older recordings when there is spare capacity.
/// </summary>
public class PipelineOptions
{
    public const string SectionName = "Pipeline";

    /// <summary>How many recent recordings to process per fast-path cycle.</summary>
    public int RecentBatchSize { get; set; } = 5;

    /// <summary>Only treat recordings received within this window as "recent".</summary>
    public int RecentLookbackMinutes { get; set; } = 10;

    /// <summary>How many older recordings to process per backfill cycle.</summary>
    public int BackfillBatchSize { get; set; } = 10;

    /// <summary>How far back the backfill will reach for unprocessed recordings.</summary>
    public int BackfillLookbackHours { get; set; } = 72;

    /// <summary>Max concurrent ASR/embed operations (bounds load on the ASR backend).</summary>
    public int MaxConcurrent { get; set; } = 2;

    /// <summary>Delay between fast-path cycles when there is recent work.</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Delay between cycles when only backfill work remains (be gentle).</summary>
    public int BackfillPollIntervalSeconds { get; set; } = 30;
}
