using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

/// <summary>
/// Shared prompt-building and window-fetching logic for the investigate agent. Used by the
/// <see cref="InvestigationWorker"/> background service so there is a single, unified way to
/// turn an anchor call (plus its surrounding radio traffic) into an agent prompt.
/// </summary>
public static class InvestigationPromptBuilder
{
    /// <summary>
    /// Fetch calls + final transcripts in a time window around a given timestamp, optionally
    /// scoped to a talkgroup. Uses cursor paging (newest-first) and stops once the window is
    /// exhausted or <paramref name="maxCalls"/> is reached.
    /// </summary>
    public static async Task<List<(CallDto Call, string? Transcript)>> FetchCallsInWindowAsync(
        ICallsService calls, ITranscriptionsService transcriptions,
        DateTimeOffset start, DateTimeOffset end, int? talkGroupId, CancellationToken ct, int maxCalls = 50)
    {
        var results = new List<(CallDto Call, string? Transcript)>();
        const int pageSize = 50;
        DateTimeOffset? cursorTime = null;
        long? cursorId = null;

        while (results.Count < maxCalls)
        {
            var page = await calls.GetCursorAsync(pageSize, cursorTime, cursorId, talkGroupId, ct: ct);
            var inWindow = page.Items
                .Where(c => c.RecordingTime >= start && c.RecordingTime <= end)
                .Take(maxCalls - results.Count)
                .ToList();

            foreach (var call in inWindow)
            {
                var transcript = call.LatestRecordingId != null
                    ? await transcriptions.GetFinalByRecordingIdAsync(call.LatestRecordingId.Value, ct)
                    : null;
                results.Add((Call: call, Transcript: transcript?.FullText));
            }

            if (!page.HasMore || inWindow.Count == 0) break;
            var last = page.Items[^1];
            cursorTime = last.RecordingTime;
            cursorId = last.Id;
        }

        return results.OrderBy(r => r.Call.RecordingTime).ToList();
    }

    /// <summary>
    /// Build the prompt for an initial investigation: the anchor call's transcript plus all
    /// calls in the surrounding window, and the user's request.
    /// </summary>
    public static string BuildInvestigationPrompt(
        CallDto anchor, string? anchorTranscript,
        List<(CallDto Call, string? Transcript)> surroundingCalls,
        int windowMinutes, string userPrompt)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Anchor Call");
        sb.AppendLine($"Call ID: {anchor.Id}");
        sb.AppendLine($"Time: {anchor.RecordingTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Talkgroup: {anchor.TalkGroupNumber} ({anchor.TalkGroupName ?? "n/a"})");
        sb.AppendLine($"Duration: {anchor.DurationSeconds:F0}s");
        sb.AppendLine();
        sb.AppendLine("Transcript:");
        sb.AppendLine(anchorTranscript ?? "(no transcript available)");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            sb.AppendLine("## User Request");
            sb.AppendLine(userPrompt);
            sb.AppendLine();
        }

        sb.AppendLine($"## Surrounding Window (±{windowMinutes} minutes)");
        sb.AppendLine($"The following {surroundingCalls.Count} calls occurred within {windowMinutes} minutes before and after the anchor call.");
        sb.AppendLine("Determine which of these are directly related to the event in the anchor call.");
        sb.AppendLine();

        foreach (var (call, transcript) in surroundingCalls)
        {
            var isAnchor = call.Id == anchor.Id;
            var anchorTag = isAnchor ? " (ANCHOR)" : "";
            sb.AppendLine($"### Call {call.Id}{anchorTag}");
            sb.AppendLine($"Time: {call.RecordingTime:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Talkgroup: {call.TalkGroupNumber} ({call.TalkGroupName ?? "n/a"})");
            sb.AppendLine($"Duration: {call.DurationSeconds:F0}s");
            sb.AppendLine($"Transcript: {transcript ?? "(no transcript available)"}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Build the prompt for a follow-up investigation: the anchor call, the parent report, and
    /// the follow-up question.
    /// </summary>
    public static string BuildFollowUpPrompt(
        CallDto anchor, string? anchorTranscript,
        string? parentResultJson, string question)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Follow-Up Investigation");
        sb.AppendLine($"You previously investigated anchor call {anchor.Id} and produced the report below.");
        sb.AppendLine("Now answer the follow-up question using your tools to gather additional data if needed.");
        sb.AppendLine("Return an additive section: additional narrative, any new related calls or entities you find.");
        sb.AppendLine("Do not repeat information already in the existing report.");
        sb.AppendLine();
        sb.AppendLine("## Anchor Call");
        sb.AppendLine($"Call ID: {anchor.Id}");
        sb.AppendLine($"Time: {anchor.RecordingTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Talkgroup: {anchor.TalkGroupNumber} ({anchor.TalkGroupName ?? "n/a"})");
        sb.AppendLine($"Transcript: {anchorTranscript ?? "(no transcript available)"}");
        sb.AppendLine();
        sb.AppendLine("## Existing Report");
        sb.AppendLine(parentResultJson ?? "(no previous report)");
        sb.AppendLine();
        sb.AppendLine("## Follow-Up Question");
        sb.AppendLine(question);
        return sb.ToString();
    }
}
