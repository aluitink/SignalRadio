namespace SignalRadio.Data.Entities;

/// <summary>
/// A background investigation request. Created when a user triggers an investigation
/// from the UI (search page, call card, or investigations page). The InvestigationWorker
/// background service picks up pending investigations, runs the AI agent, and stores
/// the result.
/// </summary>
public class Investigation
{
    public long Id { get; set; }

    /// <summary>The anchor call this investigation is about.</summary>
    public long CallId { get; set; }
    public Call Call { get; set; } = null!;

    /// <summary>Optional talkgroup scope (null = all talkgroups in the window).</summary>
    public int? TalkGroupId { get; set; }

    /// <summary>Time window in minutes before/after the anchor call.</summary>
    public int WindowMinutes { get; set; } = 30;

    /// <summary>
    /// The user's prompt/question for the investigation agent.
    /// For initial investigations this is the default "investigate this call" prompt.
    /// For follow-ups this is the specific question.
    /// </summary>
    public string Prompt { get; set; } = "";

    /// <summary>
    /// If this is a follow-up to a previous investigation, the parent investigation ID.
    /// </summary>
    public long? ParentInvestigationId { get; set; }
    public Investigation? ParentInvestigation { get; set; }

    /// <summary>
    /// Status: Pending → Running → Completed | Failed.
    /// </summary>
    public InvestigationStatus Status { get; set; } = InvestigationStatus.Pending;

    /// <summary>When the investigation was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the worker started processing.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When the investigation completed (or failed).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The serialized MAF <c>AgentSession</c> (JSON) for this investigation's chat. Persisted
    /// after every turn so the chat can be reloaded and continued across requests and restarts.
    /// </summary>
    public string? SessionJson { get; set; }

    /// <summary>Error message if the investigation failed.</summary>
    public string? Error { get; set; }

    /// <summary>Whether the user has viewed/dismissed this investigation.</summary>
    public bool IsViewed { get; set; }

    /// <summary>Follow-up investigations that reference this one as parent.</summary>
    public ICollection<Investigation> FollowUps { get; set; } = new List<Investigation>();

    /// <summary>The investigation's chat messages, in order.</summary>
    public ICollection<InvestigationChatMessage> Messages { get; set; } = new List<InvestigationChatMessage>();
}

public enum InvestigationStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}
