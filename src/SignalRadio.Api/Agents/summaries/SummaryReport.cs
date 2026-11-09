using System.ComponentModel;

namespace SignalRadio.Api.Agents;

/// <summary>
/// The structured output the summaries agent is asked to produce. The model is constrained to
/// return a JSON object matching this shape (via <c>ChatOptions</c> structured output), so the
/// orchestrator can reliably parse the summary text, a severity rating, and notable incidents.
/// </summary>
public record SummaryReport(
    [property: Description("A concise, factual, 3-6 sentence plain-prose summary of the radio traffic in the window. No markdown, no labels.")]
    string Summary,

    [property: Description("Overall severity of the traffic in the window, 0 (routine/quiet) to 10 (major emergency or critical incident).")]
    int Severity,

    [property: Description("Optional one-line headline for the most significant event, if any. Empty string if the traffic was routine.")]
    string? Headline,

    [property: Description("Notable incidents worth flagging, most important first. Empty array if none.")]
    NotableIncidentReport[] NotableIncidents);

/// <summary>A single notable incident flagged by the summaries agent.</summary>
public record NotableIncidentReport(
    [property: Description("A short factual description of the incident.")]
    string Description,

    [property: Description("How important this incident is, 0 (minor) to 10 (critical).")]
    int ImportanceScore);
