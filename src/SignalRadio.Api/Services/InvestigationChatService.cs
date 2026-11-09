using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using SignalRadio.Api.Agents;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;
using SignalRadio.Data.Entities;

namespace SignalRadio.Api.Services;

/// <summary>
/// A single event in the investigation chat stream, relayed to the UI over SignalR.
/// The <see cref="Type"/> discriminator drives client rendering:
/// <list type="bullet">
/// <item><c>text</c> — a streamed assistant text delta (append to the current message).</item>
/// <item><c>tool_call</c> — the agent invoked a tool (name + arguments).</item>
/// <item><c>tool_result</c> — a tool returned a result (truncated for display).</item>
/// <item><c>run_finished</c> — the agent turn completed; <see cref="ReportJson"/> carries the
///     structured <c>InvestigationReport</c> (if the final message parsed as JSON).</item>
/// <item><c>run_failed</c> — the agent turn failed; <see cref="Error"/> has the message.</item>
/// </list>
/// </summary>
public record InvestigationChatEvent(
    string Type,
    string? Text = null,
    string? ToolName = null,
    string? ToolArgs = null,
    string? ToolResult = null,
    string? ReportJson = null,
    string? Error = null);

/// <summary>
/// Runs the investigate agent as a resumable, streaming chat session.
///
/// The conversation state is a MAF <see cref="AgentSession"/> whose chat history is
/// serialized (via <c>AIAgent.SerializeSessionAsync</c>) and persisted to the
/// <c>Investigations.SessionJson</c> column after every turn. A new turn deserializes the
/// stored session, appends the user's message, and streams the agent's response — so the
/// chat can be reloaded from the database and continued across requests and restarts.
///
/// Streaming events (text deltas, tool calls, tool results, run finished/failed) are relayed
/// to the UI in real time over SignalR to the investigation's group. The structured
/// <c>InvestigationReport</c> is still extracted from the final message and stored in
/// <c>ResultJson</c> (the "report"), while the full chat history lives in <c>SessionJson</c>.
/// </summary>
public class InvestigationChatService(
    IServiceScopeFactory scopeFactory,
    ICallNotifier notifier,
    AgentRegistry agents,
    IChatClient chatClient,
    ILogger<InvestigationChatService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Runs one agent turn for the investigation, streaming events to the UI and persisting
    /// the updated session + report. Safe to call for the initial investigation or any
    /// follow-up question — the session is loaded from the DB (or created fresh) each time.
    /// </summary>
    public async Task RunTurnAsync(long investigationId, string userMessage, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
        var calls = scope.ServiceProvider.GetRequiredService<ICallsService>();
        var transcriptions = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();

        var inv = await db.Investigations
            .Include(i => i.Call)
            .ThenInclude(c => c!.TalkGroup)
            .Include(i => i.Call)
            .ThenInclude(c => c!.Recordings)
            .FirstOrDefaultAsync(i => i.Id == investigationId, ct)
            ?? throw new InvalidOperationException($"Investigation {investigationId} not found.");

        // Build the agent (tools capture scoped services in a closure).
        var agent = agents.Build("investigate", chatClient, () => InvestigateAgent.CreateTools(scope.ServiceProvider));

        // Load or create the session.
        AgentSession session;
        if (!string.IsNullOrWhiteSpace(inv.SessionJson))
        {
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(inv.SessionJson);
                session = await agent.DeserializeSessionAsync(element, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Investigation {Id}: failed to restore session, starting fresh.", investigationId);
                session = await agent.CreateSessionAsync(ct);
            }
        }
        else
        {
            session = await agent.CreateSessionAsync(ct);
        }

        // For the first turn, prepend the investigation context (anchor call + surrounding
        // window) as a system message so the agent has the data it needs. Subsequent turns
        // already have this context in the restored session history.
        var isFirstTurn = string.IsNullOrWhiteSpace(inv.SessionJson);
        var messages = new List<ChatMessage>();
        if (isFirstTurn)
        {
            var context = await BuildContextAsync(inv, calls, transcriptions, ct);
            if (!string.IsNullOrWhiteSpace(context))
                messages.Add(new ChatMessage(ChatRole.System, context));
        }
        messages.Add(new ChatMessage(ChatRole.User, userMessage));

        // Stream the agent's response, relaying events to the UI.
        var textBuilder = new System.Text.StringBuilder();
        try
        {
            await foreach (var update in agent.RunStreamingAsync(messages, session, cancellationToken: ct))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextContent text:
                            if (!string.IsNullOrEmpty(text.Text))
                            {
                                textBuilder.Append(text.Text);
                                await notifier.NotifyInvestigationChatEventAsync(
                                    investigationId,
                                    new InvestigationChatEvent("text", Text: text.Text), ct);
                            }
                            break;

                        case FunctionCallContent fc:
                            await notifier.NotifyInvestigationChatEventAsync(
                                investigationId,
                                new InvestigationChatEvent(
                                    "tool_call",
                                    ToolName: fc.Name,
                                    ToolArgs: FormatArguments(fc.Arguments)), ct);
                            break;

                        case FunctionResultContent fr:
                            await notifier.NotifyInvestigationChatEventAsync(
                                investigationId,
                                new InvestigationChatEvent(
                                    "tool_result",
                                    ToolResult: Truncate(fr.Result?.ToString(), 2000)), ct);
                            break;
                    }
                }
            }

            // Extract the structured report from the final text (if it parses as JSON).
            var finalText = textBuilder.ToString().Trim();
            string? reportJson = TryParseReport(finalText);

            // Persist the updated session + report and mark completed.
            var serialized = await agent.SerializeSessionAsync(session, cancellationToken: ct);
            inv.SessionJson = serialized.GetRawText();
            if (reportJson is not null)
                inv.ResultJson = reportJson;
            inv.Status = InvestigationStatus.Completed;
            inv.CompletedAt = DateTime.UtcNow;
            inv.Error = null;
            await db.SaveChangesAsync(ct);

            await notifier.NotifyInvestigationChatEventAsync(
                investigationId,
                new InvestigationChatEvent("run_finished", ReportJson: reportJson), ct);
            await notifier.NotifyInvestigationCompletedAsync(investigationId, inv.CallId, "Completed", ct);
            logger.LogInformation("Investigation {Id} turn completed ({Chars} chars).", investigationId, finalText.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Investigation {Id} turn failed.", investigationId);

            // Persist whatever session state we have so the chat is still resumable.
            try
            {
                var serialized = await agent.SerializeSessionAsync(session, cancellationToken: ct);
                inv.SessionJson = serialized.GetRawText();
            }
            catch { /* best effort */ }

            inv.Status = InvestigationStatus.Failed;
            inv.CompletedAt = DateTime.UtcNow;
            inv.Error = ex.Message;
            await db.SaveChangesAsync(ct);

            await notifier.NotifyInvestigationChatEventAsync(
                investigationId,
                new InvestigationChatEvent("run_failed", Error: ex.Message), ct);
            await notifier.NotifyInvestigationCompletedAsync(investigationId, inv.CallId, "Failed", ct);
        }
    }

    /// <summary>
    /// Builds the initial investigation context (anchor call + surrounding window) used as the
    /// system message for the first turn. Reuses the same prompt-building logic as the legacy
    /// worker so the agent sees identical data.
    /// </summary>
    private static async Task<string> BuildContextAsync(
        Investigation inv, ICallsService calls, ITranscriptionsService transcriptions, CancellationToken ct)
    {
        var anchorEntity = inv.Call;
        var latestRecording = anchorEntity.Recordings
            .OrderByDescending(r => r.ReceivedAt)
            .FirstOrDefault();
        var anchorTranscriptDto = latestRecording != null
            ? await transcriptions.GetFinalByRecordingIdAsync(latestRecording.Id, ct)
            : null;
        var anchorTranscript = anchorTranscriptDto?.FullText;

        var anchor = new Core.Dtos.CallDto(
            anchorEntity.Id,
            anchorEntity.TalkGroupId,
            anchorEntity.TalkGroup?.Number ?? "",
            anchorEntity.TalkGroup?.Name,
            anchorEntity.TalkGroup?.Description,
            anchorEntity.RecordingTime,
            anchorEntity.FrequencyHz,
            anchorEntity.DurationSeconds,
            anchorEntity.CreatedAt,
            anchorEntity.Recordings?.Count ?? 0,
            latestRecording?.Id);

        var windowStart = anchor.RecordingTime.AddMinutes(-inv.WindowMinutes);
        var windowEnd = anchor.RecordingTime.AddMinutes(inv.WindowMinutes);
        var surroundingCalls = await FetchCallsInWindowAsync(calls, transcriptions, windowStart, windowEnd, inv.TalkGroupId, ct);

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
        sb.AppendLine($"## Surrounding Window (±{inv.WindowMinutes} minutes)");
        sb.AppendLine($"The following {surroundingCalls.Count} calls occurred within {inv.WindowMinutes} minutes before and after the anchor call.");
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

    private static async Task<List<(Core.Dtos.CallDto Call, string? Transcript)>> FetchCallsInWindowAsync(
        ICallsService calls, ITranscriptionsService transcriptions,
        DateTimeOffset start, DateTimeOffset end, int? talkGroupId, CancellationToken ct)
    {
        var results = new List<(Core.Dtos.CallDto Call, string? Transcript)>();
        const int maxCalls = 50;
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
    /// Attempts to parse the agent's final text as an <see cref="InvestigationReport"/>.
    /// Returns the camelCase JSON if it parses, otherwise null (the text is still shown as a
    /// plain chat message). Tolerates markdown code fences around the JSON.
    /// </summary>
    private static string? TryParseReport(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var candidate = text.Trim();

        // Strip a leading/trailing markdown code fence if present.
        if (candidate.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = candidate.IndexOf('\n');
            if (firstNewline >= 0) candidate = candidate[(firstNewline + 1)..];
            if (candidate.EndsWith("```", StringComparison.Ordinal)) candidate = candidate[..^3];
            candidate = candidate.Trim();
        }

        // Find the outermost JSON object.
        var start = candidate.IndexOf('{');
        var end = candidate.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        candidate = candidate[start..(end + 1)];

        try
        {
            var report = JsonSerializer.Deserialize<InvestigationReport>(candidate, JsonOpts);
            if (report is null) return null;
            return JsonSerializer.Serialize(report, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatArguments(IDictionary<string, object?>? args)
    {
        if (args is null || args.Count == 0) return "{}";
        var obj = new Dictionary<string, object?>();
        foreach (var (key, value) in args)
            obj[key] = value?.ToString();
        return JsonSerializer.Serialize(obj, JsonOpts);
    }

    private static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max] + "…");
}
