using System.ComponentModel;

namespace SignalRadio.Api.Agents;

/// <summary>
/// The structured output the investigate agent produces. The model is constrained to return a
/// JSON object matching this shape (via <c>ChatOptions</c> structured output), so the controller
/// can reliably parse the event report, related calls, entities, and timeline.
/// </summary>
public record InvestigationReport(
    [property: Description("A 4-8 sentence plain-prose report of the event or activity centered on the anchor call. Describe what happened, who was involved, where, and the outcome. No markdown, no labels.")]
    string Narrative,

    [property: Description("The calls from the surrounding window that are directly related to this event, in chronological order. Include only calls that are part of or directly respond to the event — not unrelated traffic. Empty array if the anchor call is isolated.")]
    RelatedCall[] RelatedCalls,

    [property: Description("Key entities identified across the related calls: unit/vehicle identifiers, locations, cross-streets, addresses, call types, and people mentioned. Empty array if none.")]
    EntityMention[] Entities,

    [property: Description("A chronological timeline of significant events extracted from the related calls, most recent last. Empty array if the calls do not form a clear sequence.")]
    TimelineEvent[] Timeline,

    [property: Description("2-4 suggested follow-up questions or searches that would deepen the investigation. Empty array if none.")]
    string[] FollowUpQuestions,

    [property: Description("Overall assessment of the event, 0 (routine/unclear) to 10 (critical incident confirmed).")]
    int Significance);

/// <summary>A call from the surrounding window that the agent determined is related to the event.</summary>
public record RelatedCall(
    [property: Description("The call id.")]
    long CallId,

    [property: Description("The talkgroup number.")]
    string TalkGroupNumber,

    [property: Description("The talkgroup name, if known. Empty string otherwise.")]
    string TalkGroupName,

    [property: Description("ISO 8601 timestamp of the call.")]
    string RecordingTime,

    [property: Description("A one-sentence description of what this call contributes to the event.")]
    string Relevance,

    [property: Description("The full transcript text of the call.")]
    string Transcript);

/// <summary>A key entity (unit, location, person, call type) mentioned in the related calls.</summary>
public record EntityMention(
    [property: Description("The entity name or identifier, e.g. 'Unit 42', 'Main St & 5th Ave', 'MCI'.")]
    string Name,

    [property: Description("The kind of entity: 'unit', 'location', 'person', 'call-type', or 'other'.")]
    string Kind,

    [property: Description("How many related calls mention this entity.")]
    int MentionCount);

/// <summary>A single event in the chronological timeline.</summary>
public record TimelineEvent(
    [property: Description("ISO 8601 timestamp of the event, or empty string if the exact time is unknown.")]
    string Time,

    [property: Description("A short factual description of what happened.")]
    string Description,

    [property: Description("The talkgroup number the event occurred on, if known. Empty string otherwise.")]
    string TalkGroupNumber);
