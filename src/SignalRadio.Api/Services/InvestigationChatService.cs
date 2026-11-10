using System.Text;
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
/// <item><c>run_finished</c> — the agent turn completed.</item>
/// <item><c>run_failed</c> — the agent turn failed; <see cref="Error"/> has the message.</item>
/// </list>
/// </summary>
public record InvestigationChatEvent(
    string Type,
    string? Text = null,
    string? ToolName = null,
    string? ToolArgs = null,
    string? ToolResult = null,
    string? ToolCallId = null,
    string? Error = null);

/// <summary>
/// Runs the investigation agent as a single, resumable, streaming chat per investigation.
///
/// An investigation is a chat with one agent. The first turn is the user's investigation
/// prompt (anchor call + timeframe + generalized request); the agent performs the
/// investigation and produces a summary. Subsequent turns are the user's follow-up
/// questions. The user's prompt and inputs are visible in the thread as their part of the
/// conversation.
///
/// The conversation state is a MAF <see cref="AgentSession"/> whose chat history is
/// serialized (via <c>AIAgent.SerializeSessionAsync</c>) and persisted to the
/// investigation row's <c>SessionJson</c> column after every turn. A new turn deserializes
/// the stored session, appends the user's message, and streams the agent's response — so
/// the chat can be reloaded from the database and continued across requests and restarts.
///
/// Every rendered message (user input, assistant text, tool calls/results, errors) is also
/// persisted as an <see cref="InvestigationChatMessage"/> row so the full thread can be
/// reloaded when the user navigates back into the chat.
///
/// Streaming events (text deltas, tool calls, tool results, run finished/failed) are relayed
/// to the UI in real time over SignalR to the investigation's group.
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
    /// Runs one agent turn for the investigation's chat, streaming events to the UI and
    /// persisting the updated session state + rendered messages. Safe to call for the
    /// initial investigation prompt or any follow-up question — the agent session is loaded
    /// from the DB (or created fresh) each time.
    /// </summary>
    public async Task RunTurnAsync(long investigationId, string userMessage, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
        var calls = scope.ServiceProvider.GetRequiredService<ICallsService>();
        var transcriptions = scope.ServiceProvider.GetRequiredService<ITranscriptionsService>();

        var inv = await db.Investigations
            .Include(i => i.Messages)
            .Include(i => i.Call)
            .ThenInclude(c => c!.TalkGroup)
            .Include(i => i.Call)
            .ThenInclude(c => c!.Recordings)
            .FirstOrDefaultAsync(i => i.Id == investigationId, ct)
            ?? throw new InvalidOperationException($"Investigation {investigationId} not found.");

        // Build the investigation agent. The first turn (no persisted session yet) is
        // grounded in the anchor call + surrounding window as a system message; later
        // turns resume the persisted session, which already carries that context.
        var isFirstTurn = string.IsNullOrWhiteSpace(inv.SessionJson);
        var context = isFirstTurn
            ? await BuildContextAsync(inv, calls, transcriptions, ct)
            : null;

        var agent = agents.Build("investigate", chatClient,
            () => InvestigationTools.CreateDataTools(scope.ServiceProvider), context);

        AgentSession agentSession;
        if (isFirstTurn)
        {
            agentSession = await agent.CreateSessionAsync(ct);
        }
        else
        {
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(inv.SessionJson!);
                agentSession = await agent.DeserializeSessionAsync(element, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Investigation {Id}: failed to restore agent session, starting fresh.", investigationId);
                agentSession = await agent.CreateSessionAsync(ct);
            }
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, userMessage),
        };

        // Persist the user's message before running the turn so it shows up in history
        // even if the turn fails.
        await AppendMessageAsync(db, inv, "user", text: userMessage);

        // Stream the agent's response, relaying events to the UI.
        var textBuilder = new StringBuilder();
        try
        {
            await foreach (var update in agent.RunStreamingAsync(messages, agentSession, cancellationToken: ct))
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
                            await AppendMessageAsync(db, inv, "tool",
                                toolName: fc.Name, toolArgs: FormatArguments(fc.Arguments), toolCallId: fc.CallId);
                            await notifier.NotifyInvestigationChatEventAsync(
                                investigationId,
                                new InvestigationChatEvent(
                                    "tool_call",
                                    ToolName: fc.Name,
                                    ToolArgs: FormatArguments(fc.Arguments),
                                    ToolCallId: fc.CallId), ct);
                            break;

                        case FunctionResultContent fr:
                            await AttachToolResultAsync(db, inv, fr.CallId, Truncate(fr.Result?.ToString(), 2000), ct);
                            await notifier.NotifyInvestigationChatEventAsync(
                                investigationId,
                                new InvestigationChatEvent(
                                    "tool_result",
                                    ToolResult: Truncate(fr.Result?.ToString(), 2000),
                                    ToolCallId: fr.CallId), ct);
                            break;
                    }
                }
            }

            var finalText = textBuilder.ToString().Trim();

            await AppendMessageAsync(db, inv, "assistant",
                text: string.IsNullOrEmpty(finalText) ? null : finalText);

            // Persist the updated agent session and mark the investigation completed.
            var serialized = await agent.SerializeSessionAsync(agentSession, cancellationToken: ct);
            inv.SessionJson = serialized.GetRawText();
            inv.Status = InvestigationStatus.Completed;
            inv.CompletedAt = DateTime.UtcNow;
            inv.Error = null;

            await db.SaveChangesAsync(ct);

            await notifier.NotifyInvestigationChatEventAsync(
                investigationId,
                new InvestigationChatEvent("run_finished"), ct);
            await notifier.NotifyInvestigationCompletedAsync(investigationId, inv.CallId, "Completed", ct);
            logger.LogInformation("Investigation {Id} turn completed ({Chars} chars).", investigationId, finalText.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Investigation {Id} turn failed.", investigationId);

            // Persist whatever session state we have so the chat is still resumable.
            try
            {
                var serialized = await agent.SerializeSessionAsync(agentSession, cancellationToken: ct);
                inv.SessionJson = serialized.GetRawText();
            }
            catch { /* best effort */ }

            inv.Status = InvestigationStatus.Failed;
            inv.CompletedAt = DateTime.UtcNow;
            inv.Error = ex.Message;

            await AppendMessageAsync(db, inv, "assistant", error: ex.Message);

            await db.SaveChangesAsync(ct);

            await notifier.NotifyInvestigationChatEventAsync(
                investigationId,
                new InvestigationChatEvent("run_failed", Error: ex.Message), ct);
            await notifier.NotifyInvestigationCompletedAsync(investigationId, inv.CallId, "Failed", ct);
        }
    }

    /// <summary>
    /// Builds the initial investigation context (anchor call + surrounding window) used as
    /// the system message for the first turn.
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

        var sb = new StringBuilder();
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

    /// <summary>Appends a rendered message to the investigation's chat and saves it.</summary>
    private static async Task AppendMessageAsync(
        SignalRadioDbContext db, Investigation inv, string role,
        string? text = null, string? toolName = null, string? toolArgs = null,
        string? toolResult = null, string? toolCallId = null,
        string? error = null, CancellationToken ct = default)
    {
        var nextIndex = inv.Messages.Count > 0 ? inv.Messages.Max(m => m.Index) + 1 : 1;
        inv.Messages.Add(new InvestigationChatMessage
        {
            Index = nextIndex,
            Role = role,
            Text = text,
            ToolName = toolName,
            ToolArgs = toolArgs,
            ToolResult = toolResult,
            ToolCallId = toolCallId,
            Error = error,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Attaches a tool result to the matching pending tool-call message and saves it.</summary>
    private static async Task AttachToolResultAsync(
        SignalRadioDbContext db, Investigation inv, string? toolCallId, string? toolResult, CancellationToken ct)
    {
        InvestigationChatMessage? target = null;
        if (!string.IsNullOrEmpty(toolCallId))
            target = inv.Messages.FirstOrDefault(m => m.Role == "tool" && m.ToolCallId == toolCallId && m.ToolResult == null);
        target ??= inv.Messages.LastOrDefault(m => m.Role == "tool" && m.ToolResult == null);
        if (target is null) return;
        target.ToolResult = toolResult;
        await db.SaveChangesAsync(ct);
    }
}
