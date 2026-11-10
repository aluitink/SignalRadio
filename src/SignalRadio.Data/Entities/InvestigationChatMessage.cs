namespace SignalRadio.Data.Entities;

/// <summary>
/// A single rendered message in an investigation's chat, persisted so the full thread can be
/// reloaded when the user navigates back into the chat. An investigation has exactly one chat
/// (no separate sessions): the initial investigation prompt, the agent's summary, and any
/// follow-up questions all live in this thread.
/// </summary>
public class InvestigationChatMessage
{
    public long Id { get; set; }

    public long InvestigationId { get; set; }
    public Investigation Investigation { get; set; } = null!;

    /// <summary>1-based position of the message within the investigation's chat.</summary>
    public int Index { get; set; }

    /// <summary>Role: user, assistant, or tool.</summary>
    public string Role { get; set; } = "user";

    /// <summary>Message text (user input or assistant prose). Null for tool messages.</summary>
    public string? Text { get; set; }

    /// <summary>Tool name (tool messages only).</summary>
    public string? ToolName { get; set; }

    /// <summary>Tool arguments as JSON (tool messages only).</summary>
    public string? ToolArgs { get; set; }

    /// <summary>Tool result text, truncated for display (tool messages only).</summary>
    public string? ToolResult { get; set; }

    /// <summary>Correlates a tool result with its call (tool messages only).</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Error message (assistant messages only, when the turn failed).</summary>
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
