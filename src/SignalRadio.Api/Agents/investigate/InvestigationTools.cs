using System.ComponentModel;
using Microsoft.Extensions.AI;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Agents;

/// <summary>
/// Function tools for the investigation agent: data-access tools (talkgroup info, windowed
/// calls, semantic search) that let the agent discover additional information while it
/// investigates and answers the user's follow-up questions.
/// </summary>
public static class InvestigationTools
{
    /// <summary>
    /// Build the shared data-access tools, capturing the scoped services in a closure so the
    /// (static) tool methods can reach them.
    /// </summary>
    public static AIFunction[] CreateDataTools(IServiceProvider scoped)
    {
        var talkGroups = scoped.GetRequiredService<ITalkGroupsService>();
        var calls = scoped.GetRequiredService<ICallsService>();
        var transcriptions = scoped.GetRequiredService<ITranscriptionsService>();
        var search = scoped.GetRequiredService<ISearchService>();
        var ai = scoped.GetRequiredService<IAiClient>();

        async Task<string> GetTalkGroupInfo([Description("The talkgroup id.")] int talkGroupId)
        {
            var tg = await talkGroups.GetByIdAsync(talkGroupId);
            return tg == null
                ? $"Talkgroup {talkGroupId} not found."
                : $"Number: {tg.Number}, Name: {tg.Name ?? "n/a"}, AlphaTag: {tg.AlphaTag ?? "n/a"}, Description: {tg.Description ?? "n/a"}";
        }

        /// <summary>
        /// Fetch calls + transcripts in a time window around a given timestamp, optionally
        /// scoped to a talkgroup. Used when the agent needs a wider window than what was
        /// provided in the initial prompt.
        /// </summary>
        async Task<string> GetCallsInWindow(
            [Description("ISO 8601 start time of the window.")] string startTime,
            [Description("ISO 8601 end time of the window.")] string endTime,
            [Description("Optional talkgroup id to scope the window. Omit for all talkgroups.")] int? talkGroupId = null,
            [Description("Maximum number of calls to return (default 30, max 50).")] int maxCalls = 30)
        {
            var capped = Math.Clamp(maxCalls, 1, 50);
            if (!DateTimeOffset.TryParse(startTime, out var start))
                return $"Invalid start time: {startTime}";
            if (!DateTimeOffset.TryParse(endTime, out var end))
                return $"Invalid end time: {endTime}";

            // Use cursor paging to fetch calls in the window.
            var allCalls = new List<Core.Dtos.CallDto>();
            DateTimeOffset? cursorTime = null;
            long? cursorId = null;
            const int pageSize = 50;

            while (allCalls.Count < capped)
            {
                var page = await calls.GetCursorAsync(pageSize, cursorTime, cursorId, talkGroupId);
                var inWindow = page.Items
                    .Where(c => c.RecordingTime >= start && c.RecordingTime <= end)
                    .Take(capped - allCalls.Count)
                    .ToList();
                allCalls.AddRange(inWindow);

                if (!page.HasMore || inWindow.Count == 0) break;
                var last = page.Items[^1];
                cursorTime = last.RecordingTime;
                cursorId = last.Id;
            }

            if (allCalls.Count == 0)
                return $"No calls found in window {start:yyyy-MM-dd HH:mm} to {end:yyyy-MM-dd HH:mm}." +
                       (talkGroupId != null ? $" for talkgroup {talkGroupId}." : "");

            // Fetch final transcripts for each call.
            var lines = new List<string>();
            foreach (var call in allCalls.OrderBy(c => c.RecordingTime))
            {
                var transcript = call.LatestRecordingId != null
                    ? await transcriptions.GetFinalByRecordingIdAsync(call.LatestRecordingId.Value)
                    : null;
                var text = transcript?.FullText ?? "(no transcript)";
                lines.Add($"[Call {call.Id}] [{call.RecordingTime:yyyy-MM-dd HH:mm}] TG {call.TalkGroupNumber} ({call.TalkGroupName ?? "n/a"}):\n{text}");
            }

            return string.Join("\n\n---\n\n", lines);
        }

        /// <summary>
        /// Run a semantic (vector) search to find calls related to a concept or description.
        /// Useful when the agent wants to find related calls by meaning rather than exact
        /// keywords or time proximity.
        /// </summary>
        async Task<string> SemanticSearch(
            [Description("A natural-language description of what to search for, e.g. 'vehicle collision with entrapment' or 'unit 42 returning to service'.")] string query,
            [Description("Maximum number of results to return (default 10, max 20).")] int maxResults = 10)
        {
            var capped = Math.Clamp(maxResults, 1, 20);
            if (!ai.AreEmbeddingsEnabled)
                return "Semantic search is not available (embeddings disabled).";

            var results = await search.SearchAsync(query, 1, capped, semantic: true, hybrid: false);
            if (results.Transcriptions.Count == 0)
                return $"No semantically similar calls found for: {query}";

            var lines = results.Transcriptions.Select(t =>
                $"[Call {t.CallId}] [{t.RecordingTime:yyyy-MM-dd HH:mm}] TG {t.TalkGroupNumber} ({t.TalkGroupName ?? "n/a"}) [sim={t.RelevanceScore?.ToString("F3")}]:\n{t.FullText}");
            return string.Join("\n\n---\n\n", lines);
        }

        return new AIFunction[]
        {
            AIFunctionFactory.Create(GetTalkGroupInfo),
            AIFunctionFactory.Create(GetCallsInWindow),
            AIFunctionFactory.Create(SemanticSearch),
        };
    }
}
