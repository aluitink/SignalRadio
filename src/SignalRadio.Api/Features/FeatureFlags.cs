namespace SignalRadio.Api.Features;

/// <summary>
/// Feature toggles that gate the AI integrations. Bound from the "Features" config section.
///
/// - <see cref="Asr"/>: master switch for the inbound call-processing pipeline (transcription).
///   When off, no recordings are processed and no embeddings are generated (there is no text to embed).
/// - <see cref="Embeddings"/>: generate + store a vector embedding for each transcription.
///   Only meaningful when <see cref="Asr"/> is on.
/// - <see cref="Summaries"/>: run the summaries agent to produce per-talkgroup, over-time summaries.
/// </summary>
public class FeatureFlags
{
    public const string SectionName = "Features";

    public bool Asr { get; set; } = true;
    public bool Embeddings { get; set; } = true;
    public bool Summaries { get; set; } = false;
}
