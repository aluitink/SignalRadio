using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace SignalRadio.Api.Agents;

/// <summary>
/// Contract for a self-contained AI agent. Implementations supply the agent's tools and build
/// the MAF <see cref="AIAgent"/> from a chat client + the PROMPT.md instructions.
/// </summary>
public interface IAgentDefinition
{
    /// <summary>Stable identifier, e.g. "summaries". Matches the agent's folder name.</summary>
    string Name { get; }

    /// <summary>
    /// The prompt file name inside the agent's folder. Defaults to <c>PROMPT.md</c>; agents
    /// with a non-default prompt file override this.
    /// </summary>
    string PromptFileName => "PROMPT.md";

    /// <summary>
    /// Build the AIAgent. <paramref name="chatClient"/> is the OpenAI-compatible chat client;
    /// <paramref name="instructions"/> is the contents of the agent's PROMPT.md;
    /// <paramref name="tools"/> returns the agent's function tools (built with scoped services);
    /// <paramref name="extraSystemMessage"/> is optional per-run context that must be sent as a
    /// system message BEFORE the base instructions (some providers reject system messages that
    /// are not at the start of the conversation).
    /// </summary>
    AIAgent BuildAgent(IChatClient chatClient, string instructions, Func<AIFunction[]> tools, string? extraSystemMessage = null);
}
